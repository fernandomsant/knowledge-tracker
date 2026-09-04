using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Application.Authentication;

public sealed class McpAwareActionAuthorizationService(ICurrentUserContext currentUser)
    : IActionAuthorizationService
{
    public void Demand(string scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        var normalized = McpAccessTokenScopeCatalog.Normalize([scope]).Single().Value;
        if (currentUser.IsMcpAccessToken && !currentUser.Scopes.Contains(normalized, StringComparer.Ordinal))
            throw new UnauthorizedAccessException($"The MCP access token does not grant scope '{normalized}'.");
    }
}
