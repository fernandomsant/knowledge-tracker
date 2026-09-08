namespace KnowledgeTracker.Application.Authentication;

// User identity and workspace selection are separate concerns. Authentication
// establishes UserId; the request host supplies the selected WorkspaceId.
public interface ICurrentWorkspaceContext
{
    Guid? WorkspaceId { get; }

    Guid RequireWorkspaceId() => WorkspaceId is { } workspaceId && workspaceId != Guid.Empty
        ? workspaceId
        : throw new UnauthorizedAccessException("A selected workspace is required to access knowledge data.");
}
