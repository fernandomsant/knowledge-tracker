namespace KnowledgeTracker.Domain.Knowledge;

public sealed class StudyMetricDefinition
{
    public const int MaximumNameLength = 256;

    public StudyMetricDefinition(Guid id, string name, MetricNumberKind numberKind)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Study metric definition identifier is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Study metric definition name is required.", nameof(name));
        if (name.Trim().Length > MaximumNameLength)
            throw new ArgumentOutOfRangeException(nameof(name));
        if (!Enum.IsDefined(numberKind))
            throw new ArgumentOutOfRangeException(nameof(numberKind));

        Id = id;
        Name = name.Trim();
        NormalizedName = Name.ToUpperInvariant();
        NumberKind = numberKind;
    }

    public Guid Id { get; }
    public string Name { get; }
    public string NormalizedName { get; }
    public MetricNumberKind NumberKind { get; }
}
