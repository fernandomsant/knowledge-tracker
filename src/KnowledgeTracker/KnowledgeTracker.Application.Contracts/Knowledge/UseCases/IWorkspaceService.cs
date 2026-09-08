namespace KnowledgeTracker.Application.Knowledge;

// Use cases depend on this contract; Web and MCP can consume it without knowing
// how workspaces are stored or how the five-workspace rule is enforced.
public interface IWorkspaceService
{
    Task<IReadOnlyCollection<WorkspaceDetails>> ListAsync(CancellationToken ct);
    Task<WorkspaceDetails> CreateAsync(CreateWorkspaceRequest request, CancellationToken ct);
}
