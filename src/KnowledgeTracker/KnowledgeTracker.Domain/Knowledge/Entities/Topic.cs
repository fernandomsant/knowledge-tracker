namespace KnowledgeTracker.Domain.Knowledge;

/// <summary>Names the learning scope shared by notes, goals, and recorded progress.</summary>
public sealed class Topic
{
    public Topic(Guid id, Guid subjectId, string name)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Topic identifier is required.", nameof(id));
        if (subjectId == Guid.Empty)
            throw new ArgumentException("Subject identifier is required.", nameof(subjectId));
        Rename(name);
        Id = id;
        SubjectId = subjectId;
    }

    public Guid Id { get; }
    public Guid SubjectId { get; }
    public string Name { get; private set; } = string.Empty;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 256)
            throw new ArgumentException("Topic name is required and must be 256 characters or fewer.", nameof(name));
        Name = name.Trim();
    }
}
