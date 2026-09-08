using KnowledgeTracker.Application.Authentication;

namespace KnowledgeTracker.Web.Authentication.Services;

// Workspace selection is request state, not authentication state. The client
// selects a workspace with X-Workspace-Id and Data/Application verify ownership.
public sealed class CurrentWorkspaceContext(IHttpContextAccessor httpContextAccessor) : ICurrentWorkspaceContext
{
    private const string WorkspaceHeader = "X-Workspace-Id";

    public Guid? WorkspaceId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.Request.Headers[WorkspaceHeader].FirstOrDefault();
            return Guid.TryParse(value, out var workspaceId) && workspaceId != Guid.Empty
                ? workspaceId
                : null;
        }
    }

    public Guid RequireWorkspaceId()
    {
        var value = httpContextAccessor.HttpContext?.Request.Headers[WorkspaceHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(value))
            throw new UnauthorizedAccessException("A selected workspace is required to access knowledge data.");
        if (!Guid.TryParse(value, out var workspaceId) || workspaceId == Guid.Empty)
            throw new ArgumentException($"{WorkspaceHeader} must contain a valid workspace identifier.", WorkspaceHeader);

        return workspaceId;
    }
}
