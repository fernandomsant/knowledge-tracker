namespace KnowledgeTracker.Domain.Knowledge;

public sealed class SubjectSubGoal
{
    public SubjectSubGoal(Guid id, Guid subjectGoalId, string title, bool isCompleted, DateTimeOffset? completedAtUtc, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty || subjectGoalId == Guid.Empty) throw new ArgumentException("Sub-goal and parent goal identifiers are required.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 256) throw new ArgumentException("Sub-goal title is required and must be 256 characters or fewer.", nameof(title));
        if (isCompleted != (completedAtUtc is not null)) throw new ArgumentException("Completion state and completion date must match.");
        Id = id; SubjectGoalId = subjectGoalId; Title = title.Trim(); IsCompleted = isCompleted; CompletedAtUtc = completedAtUtc; CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public Guid SubjectGoalId { get; }
    public string Title { get; }
    public bool IsCompleted { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }

    public void SetCompleted(bool isCompleted, DateTimeOffset changedAtUtc)
    {
        IsCompleted = isCompleted;
        CompletedAtUtc = isCompleted ? changedAtUtc : null;
    }
}
