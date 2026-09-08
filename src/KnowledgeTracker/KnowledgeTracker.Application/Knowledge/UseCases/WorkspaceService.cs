using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

// This service coordinates the workspace use cases. The Domain entity validates
// each workspace, while the repository count protects the persisted per-user limit.
public sealed class WorkspaceService(IWorkspaceRepository workspaces, ICurrentUserContext currentUser) : IWorkspaceService
{
    public async Task<IReadOnlyCollection<WorkspaceDetails>> ListAsync(CancellationToken ct)
    {
        var userId = RequireUserId();
        return (await workspaces.ListAsync(userId, ct)).Select(ToDetails).ToArray();
    }

    public async Task<WorkspaceDetails> CreateAsync(CreateWorkspaceRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = RequireUserId();
        if (await workspaces.CountAsync(userId, ct) >= Workspace.MaximumPerUser)
            throw new InvalidOperationException($"A user cannot have more than {Workspace.MaximumPerUser} workspaces.");

        var workspace = new Workspace(Guid.NewGuid(), userId, request.Name);
        await workspaces.AddAsync(workspace, ct);
        return ToDetails(workspace);
    }

    private Guid RequireUserId() => currentUser.UserId is { } userId && userId != Guid.Empty
        ? userId
        : throw new UnauthorizedAccessException("An authenticated user is required to manage workspaces.");

    private static WorkspaceDetails ToDetails(Workspace workspace) =>
        new(workspace.Id, workspace.Name, workspace.CreatedAtUtc);
}
