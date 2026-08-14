namespace KnowledgeTracker.Domain.Knowledge;

public sealed record SubjectLayoutPosition
{
    public SubjectLayoutPosition(Guid subjectId, decimal normalizedX, decimal normalizedY)
    {
        if (subjectId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(subjectId));
        if (normalizedX is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(normalizedX));
        if (normalizedY is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(normalizedY));

        SubjectId = subjectId;
        NormalizedX = normalizedX;
        NormalizedY = normalizedY;
    }

    public Guid SubjectId { get; }
    public decimal NormalizedX { get; }
    public decimal NormalizedY { get; }
}
