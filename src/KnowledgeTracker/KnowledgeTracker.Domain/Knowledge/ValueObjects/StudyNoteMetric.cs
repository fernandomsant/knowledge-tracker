namespace KnowledgeTracker.Domain.Knowledge;

public sealed class StudyNoteMetric
{
    public StudyNoteMetric(StudyMetricDefinition definition, decimal value)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Study-note metric values cannot be negative.");
        if (definition.NumberKind == MetricNumberKind.Natural && value != decimal.Truncate(value))
            throw new ArgumentOutOfRangeException(nameof(value), "Natural-number metric values must be whole numbers.");

        Definition = definition;
        Value = value;
    }

    public StudyMetricDefinition Definition { get; }
    public decimal Value { get; }
}
