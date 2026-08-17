namespace KnowledgeTracker.Domain.Knowledge;

public sealed class SubjectGoalDayRecord
{
    public SubjectGoalDayRecord(Guid id, Guid subjectGoalId, DateOnly occurredOn, bool isCompleted, DateTimeOffset recordedAtUtc)
    {
        if (id == Guid.Empty || subjectGoalId == Guid.Empty) throw new ArgumentException("Day record and subject goal identifiers are required.");
        Id = id;
        SubjectGoalId = subjectGoalId;
        OccurredOn = occurredOn;
        IsCompleted = isCompleted;
        RecordedAtUtc = recordedAtUtc;
    }

    public Guid Id { get; }
    public Guid SubjectGoalId { get; }
    public DateOnly OccurredOn { get; }
    public bool IsCompleted { get; }
    public DateTimeOffset RecordedAtUtc { get; }
}
