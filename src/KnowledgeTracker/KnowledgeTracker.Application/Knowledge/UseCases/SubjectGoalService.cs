using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed class SubjectGoalService(ISubjectGoalRepository goals, ISubjectRepository subjects, ITopicRepository topics, IStudyNoteRepository notes, IStudyMetricDefinitionRepository definitions) : ISubjectGoalService
{
    public async Task<IReadOnlyCollection<SubjectGoalDetails>> ListBySubjectAsync(Guid subjectId, CancellationToken ct)
    {
        var subjectGoals = await goals.ListBySubjectAsync(subjectId, ct);
        var studyNotes = await notes.ListBySubjectAsync(subjectId, ct);
        var definitionMap = (await definitions.ListAsync(ct)).ToDictionary(definition => definition.Id);
        var subGoals = await goals.ListSubGoalsAsync(subjectGoals.Select(goal => goal.Id).ToArray(), ct);
        return subjectGoals.Select(goal => ToDetails(goal, studyNotes, definitionMap, subGoals.Where(item => item.SubjectGoalId == goal.Id))).ToArray();
    }

    public async Task<SubjectGoalDetails?> CreateAsync(Guid subjectId, CreateSubjectGoalRequest request, CancellationToken ct)
    {
        if (await subjects.FindAsync(subjectId, ct) is null) return null;
        var topicId = request.TopicId == Guid.Empty ? subjectId : request.TopicId;
        if (await topics.FindAsync(topicId, ct) is null) throw new ArgumentException("The selected topic does not exist.");
        if (request.Kind == GoalKind.MetricTarget && (request.MetricDefinitionId is null || await definitions.FindAsync(request.MetricDefinitionId.Value, ct) is null))
            throw new ArgumentException("The selected study metric does not exist.");

        if (request.Kind == GoalKind.MetricTarget && request.SubGoals.Count > 0) throw new ArgumentException("Metric goals cannot have sub-goals.");
        var goal = new SubjectGoal(Guid.NewGuid(), subjectId, topicId, request.Title, request.Kind, request.MetricDefinitionId, request.TargetValue, request.TargetDate, request.Period, request.PeriodStartDate, request.PeriodEndDate, long.MaxValue, false, null, DateTimeOffset.UtcNow);
        await goals.AddAsync(goal, ct);
        var subGoals = request.SubGoals.Where(title => !string.IsNullOrWhiteSpace(title)).Select(title => new SubjectSubGoal(Guid.NewGuid(), goal.Id, title, false, null, DateTimeOffset.UtcNow)).ToArray();
        await goals.AddSubGoalsAsync(subGoals, ct);
        var definitionMap = (await definitions.ListAsync(ct)).ToDictionary(definition => definition.Id);
        return ToDetails(goal, await notes.ListBySubjectAsync(subjectId, ct), definitionMap, subGoals);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => goals.DeleteAsync(id, ct);
    public Task<bool> CompleteAsync(Guid id, CancellationToken ct) => goals.CompleteAsync(id, DateTimeOffset.UtcNow, ct);
    public Task<bool> SetSubGoalCompletionAsync(Guid id, bool isCompleted, CancellationToken ct) => goals.SetSubGoalCompletionAsync(id, isCompleted, DateTimeOffset.UtcNow, ct);
    public Task<bool> SwapPriorityAsync(Guid id, Guid swapWithId, CancellationToken ct) => goals.SwapPriorityAsync(id, swapWithId, ct);

    private static SubjectGoalDetails ToDetails(SubjectGoal goal, IReadOnlyCollection<StudyNote> notes, IReadOnlyDictionary<Guid, StudyMetricDefinition> definitions, IEnumerable<SubjectSubGoal> subGoals)
    {
        definitions.TryGetValue(goal.MetricDefinitionId ?? Guid.Empty, out var definition);
        var (periodStartDate, periodEndDate) = ResolvePeriod(goal, DateOnly.FromDateTime(DateTime.UtcNow));
        decimal? currentValue = goal.Kind == GoalKind.MetricTarget
            ? notes.Where(note => note.TopicId == goal.TopicId && IsWithinPeriod(note, periodStartDate, periodEndDate)).SelectMany(note => note.Metrics).Where(metric => metric.Definition.Id == goal.MetricDefinitionId).Sum(metric => metric.Value)
            : null;
        return new(goal.Id, goal.SubjectId, goal.TopicId, goal.Title, goal.Kind, definition is null ? null : new(definition.Id, definition.Name, definition.NumberKind), goal.TargetValue, currentValue, goal.TargetDate, goal.Period, periodStartDate, periodEndDate, goal.PriorityPosition, goal.IsCompleted, goal.CompletedAtUtc, goal.CreatedAtUtc, subGoals.Select(item => new SubjectSubGoalDetails(item.Id, item.Title, item.IsCompleted, item.CompletedAtUtc)).ToArray());
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
