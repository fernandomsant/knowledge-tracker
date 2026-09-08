using KnowledgeTracker.Application.Knowledge;

namespace KnowledgeTracker.Application.Authentication;

// A user response contains the workspace choices only. The selected workspace
// determines which knowledge universe is loaded by the knowledge use cases.
public sealed record CurrentUser(Guid Id, string Login)
{
    public IReadOnlyCollection<WorkspaceDetails> Workspaces { get; init; } = [];
}
