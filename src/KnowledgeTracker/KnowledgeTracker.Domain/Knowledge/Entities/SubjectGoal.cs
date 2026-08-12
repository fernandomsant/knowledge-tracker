namespace KnowledgeTracker.Domain.Knowledge;

public sealed class SubjectGoal
{
    public SubjectGoal(Guid id, Guid subjectId, string title, GoalKind kind, Guid? metricDefinitionId, decimal? targetValue, DateOnly? targetDate, GoalPeriod period, DateOnly? customPeriodStartDate, DateOnly? customPeriodEndDate, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty || subjectId == Guid.Empty) throw new ArgumentException("Goal and subject identifiers are required.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 256) throw new ArgumentException("Goal title is required and must be 256 characters or fewer.", nameof(title));
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (kind == GoalKind.MetricTarget && (metricDefinitionId is null || targetValue is null || targetValue <= 0)) throw new ArgumentException("A metric goal needs a metric and positive target.");
        if (kind == GoalKind.TargetDate && targetDate is null) throw new ArgumentException("A date goal needs a target date.");
        if (!Enum.IsDefined(period)) throw new ArgumentOutOfRangeException(nameof(period));
        if (kind == GoalKind.TargetDate && period != GoalPeriod.AllTime) throw new ArgumentException("Only metric goals can have a period.");
        if (period == GoalPeriod.Custom && (customPeriodStartDate is null || customPeriodEndDate is null || customPeriodStartDate > customPeriodEndDate)) throw new ArgumentException("A custom period needs a valid start and end date.");
        if (period != GoalPeriod.Custom && (customPeriodStartDate is not null || customPeriodEndDate is not null)) throw new ArgumentException("Only custom goals can have a start and end date.");

        Id = id; SubjectId = subjectId; Title = title.Trim(); Kind = kind; MetricDefinitionId = metricDefinitionId;
        TargetValue = targetValue; TargetDate = targetDate; CreatedAtUtc = createdAtUtc;
        Period = period; CustomPeriodStartDate = customPeriodStartDate; CustomPeriodEndDate = customPeriodEndDate;
    }

    public Guid Id { get; }
    public Guid SubjectId { get; }
    public string Title { get; }
    public GoalKind Kind { get; }
    public Guid? MetricDefinitionId { get; }
    public decimal? TargetValue { get; }
    public DateOnly? TargetDate { get; }
    public GoalPeriod Period { get; }
    public DateOnly? CustomPeriodStartDate { get; }
    public DateOnly? CustomPeriodEndDate { get; }
    public DateTimeOffset CreatedAtUtc { get; }
}
