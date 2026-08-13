namespace KnowledgeTracker.Domain.Knowledge;

/// <summary>Names the learning scope shared by notes, goals, and recorded progress.</summary>
public sealed class Topic
{
    public Topic(Guid id, string name)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Topic identifier is required.", nameof(id));
        Rename(name);
        Id = id;
    }

    public Guid Id { get; }
    public string Name { get; private set; } = string.Empty;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 256)
            throw new ArgumentException("Topic name is required and must be 256 characters or fewer.", nameof(name));
        Name = name.Trim();
    }
}
