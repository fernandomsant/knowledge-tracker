namespace KnowledgeTracker.Application.Authentication;

public interface ICurrentUserContext
{
    Guid? UserId { get; }
    bool IsMcpAccessToken => false;
    IReadOnlySet<string> Scopes => new HashSet<string>(StringComparer.Ordinal);
}
