namespace KnowledgeTracker.Domain.Knowledge;

public enum GoalCompletionSource : byte
{
    Manual = 1,
    Metric = 2,
    SubGoals = 3,
    Backfill = 4
}

public sealed class SubjectGoalCompletion
{
    public SubjectGoalCompletion(
        Guid id,
        Guid subjectGoalId,
        DateOnly occurrenceStartDate,
        DateOnly occurrenceEndDate,
        DateTimeOffset completedAtUtc,
        GoalCompletionSource source)
    {
        if (id == Guid.Empty || subjectGoalId == Guid.Empty)
            throw new ArgumentException("Completion and goal identifiers are required.");
        if (occurrenceStartDate > occurrenceEndDate)
            throw new ArgumentException("An occurrence must start on or before it ends.");
        if (!Enum.IsDefined(source))
            throw new ArgumentOutOfRangeException(nameof(source));

        Id = id;
        SubjectGoalId = subjectGoalId;
        OccurrenceStartDate = occurrenceStartDate;
        OccurrenceEndDate = occurrenceEndDate;
        CompletedAtUtc = completedAtUtc.ToUniversalTime();
        Source = source;
    }

    public Guid Id { get; }
    public Guid SubjectGoalId { get; }
    public DateOnly OccurrenceStartDate { get; }
    public DateOnly OccurrenceEndDate { get; }
    public DateTimeOffset CompletedAtUtc { get; }
    public GoalCompletionSource Source { get; }
}
