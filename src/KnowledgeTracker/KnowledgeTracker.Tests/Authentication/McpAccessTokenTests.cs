using KnowledgeTracker.Domain.Authentication;
using Xunit;

namespace KnowledgeTracker.Tests.Authentication;

public sealed class McpAccessTokenTests
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreatesTokenWithNormalizedDistinctScopes()
    {
        var token = CreateToken([new("NOTES:READ"), new("notes:read"), new("subjects:write")]);

        Assert.Equal(["notes:read", "subjects:write"], token.Scopes.Select(scope => scope.Value));
        Assert.True(token.IsActive(CreatedAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void RejectsUnknownScope()
    {
        Assert.Throws<ArgumentException>(() => McpAccessTokenScopeCatalog.Normalize(["users:delete"]));
    }

    [Fact]
    public void RejectsExpirationAtOrBeforeCreation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateToken([new(McpAccessTokenScopeCatalog.NotesRead)], CreatedAtUtc));
    }

    [Fact]
    public void RevocationIsIrreversibleAndMakesTokenInactive()
    {
        var token = CreateToken([new(McpAccessTokenScopeCatalog.NotesRead)]);

        token.Revoke(CreatedAtUtc.AddMinutes(1));
        token.Revoke(CreatedAtUtc.AddMinutes(2));

        Assert.Equal(CreatedAtUtc.AddMinutes(1), token.RevokedAtUtc);
        Assert.False(token.IsActive(CreatedAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void RequiresAtLeastOneScope()
    {
        Assert.Throws<ArgumentException>(() => CreateToken([]));
    }

    private static McpAccessToken CreateToken(
        IEnumerable<McpAccessTokenScope> scopes,
        DateTimeOffset? expiresAtUtc = null
    ) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "MCP client",
        "token-identifier",
        "secret-hash",
        CreatedAtUtc,
        expiresAtUtc ?? CreatedAtUtc.AddDays(30),
        scopes
    );
}
