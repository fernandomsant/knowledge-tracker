using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed class SubjectGoalService(ISubjectGoalRepository goals, ISubjectRepository subjects, IStudyNoteRepository notes, IStudyMetricDefinitionRepository definitions) : ISubjectGoalService
{
    public async Task<IReadOnlyCollection<SubjectGoalDetails>> ListBySubjectAsync(Guid subjectId, CancellationToken ct)
    {
        var subjectGoals = await goals.ListBySubjectAsync(subjectId, ct);
        var studyNotes = await notes.ListBySubjectAsync(subjectId, ct);
        var definitionMap = (await definitions.ListAsync(ct)).ToDictionary(definition => definition.Id);
        return subjectGoals.Select(goal => ToDetails(goal, studyNotes, definitionMap)).ToArray();
    }

    public async Task<SubjectGoalDetails?> CreateAsync(Guid subjectId, CreateSubjectGoalRequest request, CancellationToken ct)
    {
        if (await subjects.FindAsync(subjectId, ct) is null) return null;
        if (request.Kind == GoalKind.MetricTarget && (request.MetricDefinitionId is null || await definitions.FindAsync(request.MetricDefinitionId.Value, ct) is null))
            throw new ArgumentException("The selected study metric does not exist.");

        var goal = new SubjectGoal(Guid.NewGuid(), subjectId, request.Title, request.Kind, request.MetricDefinitionId, request.TargetValue, request.TargetDate, request.Period, request.PeriodStartDate, request.PeriodEndDate, DateTimeOffset.UtcNow);
        await goals.AddAsync(goal, ct);
        var definitionMap = (await definitions.ListAsync(ct)).ToDictionary(definition => definition.Id);
        return ToDetails(goal, await notes.ListBySubjectAsync(subjectId, ct), definitionMap);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => goals.DeleteAsync(id, ct);

    private static SubjectGoalDetails ToDetails(SubjectGoal goal, IReadOnlyCollection<StudyNote> notes, IReadOnlyDictionary<Guid, StudyMetricDefinition> definitions)
    {
        definitions.TryGetValue(goal.MetricDefinitionId ?? Guid.Empty, out var definition);
        var (periodStartDate, periodEndDate) = ResolvePeriod(goal, DateOnly.FromDateTime(DateTime.UtcNow));
        decimal? currentValue = goal.Kind == GoalKind.MetricTarget
            ? notes.Where(note => IsWithinPeriod(note, periodStartDate, periodEndDate)).SelectMany(note => note.Metrics).Where(metric => metric.Definition.Id == goal.MetricDefinitionId).Sum(metric => metric.Value)
            : null;
        return new(goal.Id, goal.SubjectId, goal.Title, goal.Kind, definition is null ? null : new(definition.Id, definition.Name, definition.NumberKind), goal.TargetValue, currentValue, goal.TargetDate, goal.Period, periodStartDate, periodEndDate, goal.CreatedAtUtc);
    }

    private static bool IsWithinPeriod(StudyNote note, DateOnly? startDate, DateOnly? endDate)
    {
        var studiedOn = DateOnly.FromDateTime(note.StudyStartedAtUtc.UtcDateTime);
        return (startDate is null || studiedOn >= startDate) && (endDate is null || studiedOn <= endDate);
    }

    private static (DateOnly? StartDate, DateOnly? EndDate) ResolvePeriod(SubjectGoal goal, DateOnly today) => goal.Period switch
    {
        GoalPeriod.Daily => (today, today),
        GoalPeriod.Weekly => (today.AddDays(-((int)(today.DayOfWeek + 6) % 7)), today.AddDays(6 - (int)(today.DayOfWeek + 6) % 7)),
        GoalPeriod.Monthly => (new DateOnly(today.Year, today.Month, 1), new DateOnly(today.Year, today.Month, 1).AddMonths(1).AddDays(-1)),
        GoalPeriod.Custom => (goal.CustomPeriodStartDate, goal.CustomPeriodEndDate),
        _ => (null, null)
    };
}
