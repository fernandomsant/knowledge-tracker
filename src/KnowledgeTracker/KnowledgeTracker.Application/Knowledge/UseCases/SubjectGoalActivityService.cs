using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed class SubjectGoalActivityService(
    ISubjectGoalActivityRepository activityGoals,
    ISubjectGoalCompletionRepository completions,
    ISubjectGoalRepository goals,
    IStudyNoteRepository notes,
    IStudyMetricDefinitionRepository definitions) : ISubjectGoalActivityService
{
    private static readonly DateOnly MinimumDate = new(2000, 1, 1);
    private const int MaximumRangeDays = 5000;

    public async Task<IReadOnlyCollection<GoalActivityDetails>> GetAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        ValidateRange(from, to);
        var goalList = await activityGoals.ListForPeriodAsync(from, to, ct);
        if (goalList.Count == 0) return [];
        var registry = (await completions.ListAsync(goalList.Select(goal => goal.Id).ToArray(), from, to, ct))
            .ToDictionary(completion => (completion.SubjectGoalId, completion.OccurrenceStartDate, completion.OccurrenceEndDate));
        return goalList.SelectMany(goal => GoalOccurrenceCalculator.GetOccurrences(goal, from, to, DateOnly.FromDateTime(DateTime.UtcNow))
            .Select(occurrence => registry.TryGetValue((goal.Id, occurrence.StartDate, occurrence.EndDate), out var completion)
                ? new GoalActivityDetails(goal.Id, goal.SubjectId, goal.TopicId, goal.Title, goal.Kind, goal.Period, occurrence.StartDate, occurrence.EndDate, completion.CompletedAtUtc)
                : new GoalActivityDetails(goal.Id, goal.SubjectId, goal.TopicId, goal.Title, goal.Kind, goal.Period, occurrence.StartDate, occurrence.EndDate, null)))
            .OrderBy(row => row.OccurrenceStartDate).ThenBy(row => row.GoalTitle).ToArray();
    }

    public async Task ReevaluateMetricGoalsAsync(Guid subjectId, CancellationToken ct, DateOnly? affectedDate = null)
    {
        var subjectGoals = (await goals.ListBySubjectAsync(subjectId, ct)).Where(goal => goal.Kind == GoalKind.MetricTarget).ToArray();
        if (subjectGoals.Length == 0) return;
        var studyNotes = await notes.ListBySubjectAsync(subjectId, ct);
        var metricDefinitions = (await definitions.ListAsync(ct)).ToDictionary(definition => definition.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var goal in subjectGoals)
        {
            var start = DateOnly.FromDateTime(goal.CreatedAtUtc.UtcDateTime);
            var occurrences = GoalOccurrenceCalculator.GetOccurrences(goal, start, today, today)
                .Where(occurrence => affectedDate is null || (affectedDate >= occurrence.StartDate && affectedDate <= occurrence.EndDate))
                .ToArray();
            foreach (var occurrence in occurrences)
            {
                var total = studyNotes
                    .Where(note => note.TopicId == goal.TopicId && IsWithin(note, occurrence))
                    .Sum(note => MetricValue(note, goal, metricDefinitions));
                if (total >= goal.TargetValue!.Value)
                    await completions.RegisterAsync(new SubjectGoalCompletion(Guid.NewGuid(), goal.Id, occurrence.StartDate, occurrence.EndDate, DateTimeOffset.UtcNow, GoalCompletionSource.Metric), ct);
                else
                    await completions.RemoveAsync(goal.Id, occurrence.StartDate, occurrence.EndDate, ct);
            }
        }
    }

    private static decimal MetricValue(StudyNote note, SubjectGoal goal, IReadOnlyDictionary<Guid, StudyMetricDefinition> definitions)
    {
        if (goal.MetricDefinitionId is not null && definitions.TryGetValue(goal.MetricDefinitionId.Value, out var definition) && definition.NormalizedName == StandardStudyMetricDefinitionIds.StudyTimeNormalizedName)
            return note.StudyDuration.Ticks / (decimal)TimeSpan.TicksPerHour;
        return note.Metrics.FirstOrDefault(metric => metric.Definition.Id == goal.MetricDefinitionId)?.Value ?? 0;
    }

    private static bool IsWithin(StudyNote note, GoalOccurrence occurrence)
    {
        var date = DateOnly.FromDateTime(note.StudyStartedAtUtc.UtcDateTime);
        return date >= occurrence.StartDate && date <= occurrence.EndDate;
    }

    private static void ValidateRange(DateOnly from, DateOnly to)
    {
        if (from < MinimumDate || from > to || to.DayNumber - from.DayNumber > MaximumRangeDays)
            throw new ArgumentException($"The requested date range must be between {MinimumDate:yyyy-MM-dd} and {MaximumRangeDays} days.");
    }
}
