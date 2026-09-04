using System.Security.Claims;
using KnowledgeTracker.Application.Authentication;

namespace KnowledgeTracker.Mcp.Authentication.Services;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        ? userId
        : null;

    public bool IsMcpAccessToken => User?.HasClaim("authentication_method", "mcp_access_token") == true;

    public IReadOnlySet<string> Scopes => User is null
        ? new HashSet<string>(StringComparer.Ordinal)
        : User.FindAll("mcp_scope").Select(claim => claim.Value).ToHashSet(StringComparer.Ordinal);
}
