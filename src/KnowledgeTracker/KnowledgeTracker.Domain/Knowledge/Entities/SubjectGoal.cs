namespace KnowledgeTracker.Domain.Knowledge;

public sealed class SubjectGoal
{
    public SubjectGoal(Guid id, Guid subjectId, string title, GoalKind kind, Guid? metricDefinitionId, decimal? targetValue, DateOnly? targetDate, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty || subjectId == Guid.Empty) throw new ArgumentException("Goal and subject identifiers are required.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 256) throw new ArgumentException("Goal title is required and must be 256 characters or fewer.", nameof(title));
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (kind == GoalKind.MetricTarget && (metricDefinitionId is null || targetValue is null || targetValue <= 0)) throw new ArgumentException("A metric goal needs a metric and positive target.");
        if (kind == GoalKind.TargetDate && targetDate is null) throw new ArgumentException("A date goal needs a target date.");

        Id = id; SubjectId = subjectId; Title = title.Trim(); Kind = kind; MetricDefinitionId = metricDefinitionId;
        TargetValue = targetValue; TargetDate = targetDate; CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public Guid SubjectId { get; }
    public string Title { get; }
    public GoalKind Kind { get; }
    public Guid? MetricDefinitionId { get; }
    public decimal? TargetValue { get; }
    public DateOnly? TargetDate { get; }
    public DateTimeOffset CreatedAtUtc { get; }
}
