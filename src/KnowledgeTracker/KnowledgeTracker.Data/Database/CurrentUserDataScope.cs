using KnowledgeTracker.Application.Authentication;

namespace KnowledgeTracker.Data.Database;

public sealed class CurrentUserDataScope(ICurrentUserContext currentUser)
{
    public Guid RequireUserId() => currentUser.UserId is { } userId && userId != Guid.Empty
        ? userId
        : throw new UnauthorizedAccessException("An authenticated user is required to access knowledge data.");
}
