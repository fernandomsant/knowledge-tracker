namespace KnowledgeTracker.Domain.Knowledge;

public sealed class Workspace
{
    public const int MaximumPerUser = 5;

    public Workspace(Guid id, Guid userId, string name, DateTimeOffset? createdAtUtc = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Workspace identifier is required.", nameof(id));
        if (userId == Guid.Empty)
            throw new ArgumentException("Workspace user identifier is required.", nameof(userId));

        Id = id;
        UserId = userId;
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
        Rename(name);
    }

    // A workspace is the boundary for one user's knowledge universe. Subjects point
    // to it, while the rest of the knowledge graph reaches the workspace through its subject.
    public Guid Id { get; }
    public Guid UserId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public string Name { get; private set; } = string.Empty;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workspace name is required.", nameof(name));
        if (name.Trim().Length > 256)
            throw new ArgumentOutOfRangeException(nameof(name));

        Name = name.Trim();
    }
}
