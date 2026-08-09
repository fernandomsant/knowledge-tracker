using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Application.Authentication;

public interface IAccessTokenService
{
    AccessToken Issue(AccessToken unsignedToken);
    AccessToken? Validate(AccessTokenReference token);
}