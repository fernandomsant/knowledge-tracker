using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Domain.Authentication;

public sealed class User
{
    private readonly List<Workspace> workspaces = [];

    public Guid Id { get; init; } = Guid.NewGuid();
    public string Login { get; init; } = "";
    public string PasswordHash { get; init; } = "";
    public string NormalizedLogin => Login.Trim().ToUpperInvariant();
    public IReadOnlyCollection<Workspace> Workspaces => workspaces.AsReadOnly();

    public Workspace CreateWorkspace(string name)
    {
        if (workspaces.Count >= Workspace.MaximumPerUser)
            throw new InvalidOperationException($"A user cannot have more than {Workspace.MaximumPerUser} workspaces.");

        var workspace = new Workspace(Guid.NewGuid(), Id, name);
        workspaces.Add(workspace);
        return workspace;
    }
}
