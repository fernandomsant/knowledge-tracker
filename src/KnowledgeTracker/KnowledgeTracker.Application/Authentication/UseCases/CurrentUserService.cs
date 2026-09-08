using KnowledgeTracker.Application.Knowledge;

namespace KnowledgeTracker.Application.Authentication;

// User data and workspace data are composed here so callers do not need to
// understand the separate authentication and knowledge persistence boundaries.
public sealed class CurrentUserService(IUserRepository users, IWorkspaceRepository? workspaces = null) : ICurrentUserService
{
    public async Task<CurrentUser?> GetAsync(Guid id, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id, ct);
        if (user is null)
            return null;

        var currentUser = new CurrentUser(user.Id, user.Login);
        if (workspaces is null)
            return currentUser;

        return currentUser with
        {
            Workspaces = (await workspaces.ListAsync(user.Id, ct))
                .Select(workspace => new WorkspaceDetails(workspace.Id, workspace.Name, workspace.CreatedAtUtc))
                .ToArray()
        };
    }
}
