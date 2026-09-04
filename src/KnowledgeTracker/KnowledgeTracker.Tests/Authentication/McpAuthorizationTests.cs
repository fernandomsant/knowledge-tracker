using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Domain.Authentication;
using Xunit;

namespace KnowledgeTracker.Tests.Authentication;

public sealed class McpAuthorizationTests
{
    [Fact]
    public void Demand_AllowsGrantedScope()
    {
        var authorization = new McpAwareActionAuthorizationService(
            new FakeCurrentUserContext(true, McpAccessTokenScopeCatalog.NotesRead));

        authorization.Demand(McpAccessTokenScopeCatalog.NotesRead);
    }

    [Fact]
    public void Demand_RejectsScopeMissingFromDelegatedToken()
    {
        var authorization = new McpAwareActionAuthorizationService(
            new FakeCurrentUserContext(true, McpAccessTokenScopeCatalog.NotesRead));

        Assert.Throws<UnauthorizedAccessException>(() =>
            authorization.Demand(McpAccessTokenScopeCatalog.NotesWrite));
    }

    private sealed class FakeCurrentUserContext(bool isMcpAccessToken, params string[] scopes) : ICurrentUserContext
    {
        public Guid? UserId => Guid.NewGuid();
        public bool IsMcpAccessToken { get; } = isMcpAccessToken;
        public IReadOnlySet<string> Scopes { get; } = scopes.ToHashSet(StringComparer.Ordinal);
    }
}
