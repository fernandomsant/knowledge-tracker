namespace KnowledgeTracker.Domain.Knowledge;

public sealed class StudyMetricDefinition
{
    public const int MaximumNameLength = 256;

    public StudyMetricDefinition(Guid id, string name, MetricNumberKind numberKind)
        : this(id, null, name, numberKind)
    {
    }

    public StudyMetricDefinition(Guid id, Guid userId, string name, MetricNumberKind numberKind)
        : this(id, (Guid?)userId, name, numberKind)
    {
    }

    private StudyMetricDefinition(Guid id, Guid? userId, string name, MetricNumberKind numberKind)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Study metric definition identifier is required.", nameof(id));
        if (userId is { } requiredUserId && requiredUserId == Guid.Empty)
            throw new ArgumentException("Study metric definition user identifier is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Study metric definition name is required.", nameof(name));
        if (name.Trim().Length > MaximumNameLength)
            throw new ArgumentOutOfRangeException(nameof(name));
        if (!Enum.IsDefined(numberKind))
            throw new ArgumentOutOfRangeException(nameof(numberKind));

        Id = id;
        UserId = userId ?? Guid.Empty;
        Name = name.Trim();
        NormalizedName = Name.ToUpperInvariant();
        NumberKind = numberKind;
    }

    public Guid Id { get; }
    public Guid UserId { get; }
    public string Name { get; }
    public string NormalizedName { get; }
    public MetricNumberKind NumberKind { get; }
}
