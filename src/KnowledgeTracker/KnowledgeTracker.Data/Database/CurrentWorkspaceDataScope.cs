using KnowledgeTracker.Application.Authentication;

namespace KnowledgeTracker.Data.Database;

// Knowledge data is owned by both the authenticated user and the selected
// workspace. Repositories use this scope so every read and write applies the
// same ownership boundary.
public sealed class CurrentWorkspaceDataScope(
    CurrentUserDataScope userScope,
    ICurrentWorkspaceContext currentWorkspace
)
{
    public Guid RequireUserId() => userScope.RequireUserId();

    public Guid RequireWorkspaceId() => currentWorkspace.RequireWorkspaceId();
}
