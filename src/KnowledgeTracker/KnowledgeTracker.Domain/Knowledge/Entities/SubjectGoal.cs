namespace KnowledgeTracker.Domain.Knowledge;

public sealed class SubjectGoal
{
    public SubjectGoal(Guid id, Guid subjectId, Guid topicId, string title, GoalKind kind, Guid? metricDefinitionId, decimal? targetValue, DateOnly? targetDate, GoalPeriod period, DateOnly? customPeriodStartDate, DateOnly? customPeriodEndDate, long priorityPosition, bool isCompleted, DateTimeOffset? completedAtUtc, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty || subjectId == Guid.Empty || topicId == Guid.Empty) throw new ArgumentException("Goal, subject, and topic identifiers are required.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 256) throw new ArgumentException("Goal title is required and must be 256 characters or fewer.", nameof(title));
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (kind == GoalKind.MetricTarget && (metricDefinitionId is null || targetValue is null || targetValue <= 0)) throw new ArgumentException("A metric goal needs a metric and positive target.");
        if (kind == GoalKind.TargetDate && (metricDefinitionId is not null || targetValue is not null)) throw new ArgumentException("A completion goal cannot use a metric target.");
        if (!Enum.IsDefined(period)) throw new ArgumentOutOfRangeException(nameof(period));
        if (priorityPosition < 1) throw new ArgumentOutOfRangeException(nameof(priorityPosition));
        if (period == GoalPeriod.Custom && (customPeriodStartDate is null || customPeriodEndDate is null || customPeriodStartDate > customPeriodEndDate)) throw new ArgumentException("A custom period needs a valid start and end date.");
        if (period != GoalPeriod.Custom && (customPeriodStartDate is not null || customPeriodEndDate is not null)) throw new ArgumentException("Only custom goals can have a start and end date.");
        if (kind == GoalKind.MetricTarget && (isCompleted || completedAtUtc is not null)) throw new ArgumentException("Metric goals are completed by reaching their target.");
        if (isCompleted != (completedAtUtc is not null)) throw new ArgumentException("Completion state and completion date must match.");

        Id = id; SubjectId = subjectId; TopicId = topicId; Title = title.Trim(); Kind = kind; MetricDefinitionId = metricDefinitionId;
        TargetValue = targetValue; TargetDate = targetDate; CreatedAtUtc = createdAtUtc;
        Period = period; CustomPeriodStartDate = customPeriodStartDate; CustomPeriodEndDate = customPeriodEndDate; PriorityPosition = priorityPosition;
        IsCompleted = isCompleted; CompletedAtUtc = completedAtUtc;
    }

    public Guid Id { get; }
    public Guid SubjectId { get; }
    public Guid TopicId { get; }
    public string Title { get; }
    public GoalKind Kind { get; }
    public Guid? MetricDefinitionId { get; }
    public decimal? TargetValue { get; }
    public DateOnly? TargetDate { get; }
    public GoalPeriod Period { get; }
    public DateOnly? CustomPeriodStartDate { get; }
    public DateOnly? CustomPeriodEndDate { get; }
    public long PriorityPosition { get; }
    public bool IsCompleted { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }

    public SubjectGoal(Guid id, Guid subjectId, string title, GoalKind kind, Guid? metricDefinitionId, decimal? targetValue, DateOnly? targetDate, GoalPeriod period, DateOnly? customPeriodStartDate, DateOnly? customPeriodEndDate, long priorityPosition, bool isCompleted, DateTimeOffset? completedAtUtc, DateTimeOffset createdAtUtc)
        : this(id, subjectId, subjectId, title, kind, metricDefinitionId, targetValue, targetDate, period, customPeriodStartDate, customPeriodEndDate, priorityPosition, isCompleted, completedAtUtc, createdAtUtc)
    {
    }

    public void Complete(DateTimeOffset completedAtUtc)
    {
        if (Kind == GoalKind.MetricTarget) throw new InvalidOperationException("Metric goals cannot be manually completed.");
        if (IsCompleted) return;
        IsCompleted = true; CompletedAtUtc = completedAtUtc;
    }
}
