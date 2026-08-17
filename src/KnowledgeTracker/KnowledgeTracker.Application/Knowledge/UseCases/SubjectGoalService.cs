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
        if (request.TopicId == Guid.Empty)
            throw new ArgumentException("A topic must be selected.", nameof(request));
        var topicId = request.TopicId;
        var topic = await topics.FindAsync(topicId, ct);
        if (topic is null || topic.SubjectId != subjectId)
            throw new ArgumentException("The selected topic does not belong to this subject.", nameof(request));
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

    public async Task<SubjectGoalDetails?> UpdateAsync(Guid id, UpdateSubjectGoalRequest request, CancellationToken ct)
    {
        var existing = await goals.FindAsync(id, ct);
        if (existing is null) return null;
        if (request.TopicId == Guid.Empty) throw new ArgumentException("A topic must be selected.", nameof(request));
        var topic = await topics.FindAsync(request.TopicId, ct);
        if (topic is null || topic.SubjectId != existing.SubjectId)
            throw new ArgumentException("The selected topic does not belong to this subject.", nameof(request));
        if (request.Kind == GoalKind.MetricTarget && (request.MetricDefinitionId is null || await definitions.FindAsync(request.MetricDefinitionId.Value, ct) is null))
            throw new ArgumentException("The selected study metric does not exist.");
        if (request.Kind == GoalKind.MetricTarget && request.SubGoals.Count > 0)
            throw new ArgumentException("Metric goals cannot have sub-goals.");

        var kindChanged = existing.Kind != request.Kind;
        var updated = new SubjectGoal(existing.Id, existing.SubjectId, request.TopicId, request.Title, request.Kind, request.MetricDefinitionId, request.TargetValue, request.TargetDate, request.Period, request.PeriodStartDate, request.PeriodEndDate, existing.PriorityPosition, kindChanged ? false : existing.IsCompleted, kindChanged ? null : existing.CompletedAtUtc, existing.CreatedAtUtc);
        var existingSubGoals = await goals.ListSubGoalsAsync([id], ct);
        var subGoals = request.Kind == GoalKind.TargetDate ? ReconcileSubGoals(id, request.SubGoals, existingSubGoals) : [];
        await goals.UpdateAsync(updated, subGoals, ct);

        var definitionMap = (await definitions.ListAsync(ct)).ToDictionary(definition => definition.Id);
        return ToDetails(updated, await notes.ListBySubjectAsync(existing.SubjectId, ct), definitionMap, subGoals);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => goals.DeleteAsync(id, ct);
    public Task<bool> CompleteAsync(Guid id, CancellationToken ct) => goals.CompleteAsync(id, DateTimeOffset.UtcNow, ct);
    public Task<bool> SetSubGoalCompletionAsync(Guid id, bool isCompleted, CancellationToken ct) => goals.SetSubGoalCompletionAsync(id, isCompleted, DateTimeOffset.UtcNow, ct);
    public Task<bool> SwapPriorityAsync(Guid id, Guid swapWithId, CancellationToken ct) => goals.SwapPriorityAsync(id, swapWithId, ct);

    private static IReadOnlyCollection<SubjectSubGoal> ReconcileSubGoals(Guid goalId, IReadOnlyCollection<string> requestedTitles, IReadOnlyCollection<SubjectSubGoal> existingSubGoals)
    {
        var reusable = existingSubGoals
            .GroupBy(subGoal => subGoal.Title, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => new Queue<SubjectSubGoal>(group), StringComparer.Ordinal);

        return requestedTitles
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Select(title =>
            {
                var normalizedTitle = title.Trim();
                if (reusable.TryGetValue(normalizedTitle, out var matches) && matches.TryDequeue(out var existing))
                    return new SubjectSubGoal(existing.Id, goalId, normalizedTitle, existing.IsCompleted, existing.CompletedAtUtc, existing.CreatedAtUtc);
                return new SubjectSubGoal(Guid.NewGuid(), goalId, normalizedTitle, false, null, DateTimeOffset.UtcNow);
            })
            .ToArray();
    }

    private static SubjectGoalDetails ToDetails(SubjectGoal goal, IReadOnlyCollection<StudyNote> notes, IReadOnlyDictionary<Guid, StudyMetricDefinition> definitions, IEnumerable<SubjectSubGoal> subGoals)
    {
        definitions.TryGetValue(goal.MetricDefinitionId ?? Guid.Empty, out var definition);
        var (periodStartDate, periodEndDate) = ResolvePeriod(goal, DateOnly.FromDateTime(DateTime.UtcNow));
        var scopedNotes = notes.Where(note =>
            note.TopicId == goal.TopicId && IsWithinPeriod(note, periodStartDate, periodEndDate)
        );
        decimal? currentValue = goal.Kind == GoalKind.MetricTarget
            ? definition?.NormalizedName == StandardStudyMetricDefinitionIds.StudyTimeNormalizedName
                ? scopedNotes.Sum(note => note.StudyDuration.Ticks / (decimal)TimeSpan.TicksPerHour)
                : scopedNotes.SelectMany(note => note.Metrics).Where(metric => metric.Definition.Id == goal.MetricDefinitionId).Sum(metric => metric.Value)
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
