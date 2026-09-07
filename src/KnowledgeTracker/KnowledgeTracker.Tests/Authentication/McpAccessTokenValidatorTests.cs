using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Domain.Authentication;
using KnowledgeTracker.Infrastructure.Authentication;
using KnowledgeTracker.Infrastructure.Authentication.Services.AccessTokens;
using Xunit;

namespace KnowledgeTracker.Tests.Authentication;

public sealed class McpAccessTokenValidatorTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ValidateAsync_ReturnsPersistedTokenScopes()
    {
        var token = CreateToken();
        var repository = new FakeTokenRepository(token);
        var validator = CreateValidator(repository);

        var result = await validator.ValidateAsync("mcp_identifier_secret", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(UserId, result.UserId);
        Assert.Equal(token.Id, result.TokenId);
        Assert.Equal([McpAccessTokenScopeCatalog.NotesRead, McpAccessTokenScopeCatalog.SubjectsRead], result.Scopes);
        Assert.Equal(Now, token.LastUsedAtUtc);
        Assert.Same(token, Assert.Single(repository.Updated));
    }

    [Theory]
    [InlineData("")]
    [InlineData("mcp_identifier")]
    [InlineData("mcp_identifier_secret_extra")]
    [InlineData("other_identifier_secret")]
    public async Task ValidateAsync_RejectsMalformedToken(string value)
    {
        var validator = CreateValidator(new FakeTokenRepository(CreateToken()));

        Assert.Null(await validator.ValidateAsync(value, CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_RejectsWrongSecretAndDoesNotMarkTokenUsed()
    {
        var token = CreateToken();
        var repository = new FakeTokenRepository(token);
        var validator = CreateValidator(repository);

        Assert.Null(await validator.ValidateAsync("mcp_identifier_wrong", CancellationToken.None));
        Assert.Null(token.LastUsedAtUtc);
        Assert.Empty(repository.Updated);
    }

    [Fact]
    public async Task ValidateAsync_RejectsExpiredAndRevokedTokens()
    {
        var expired = CreateToken(expiresAtUtc: Now.AddMinutes(-1));
        Assert.Null(await CreateValidator(new FakeTokenRepository(expired))
            .ValidateAsync("mcp_identifier_secret", CancellationToken.None));

        var revoked = CreateToken();
        revoked.Revoke(Now.AddMinutes(-1));
        Assert.Null(await CreateValidator(new FakeTokenRepository(revoked))
            .ValidateAsync("mcp_identifier_secret", CancellationToken.None));
    }

    [Fact]
    public void Generator_CreatesUniqueOpaqueTokens()
    {
        var generator = new OpaqueMcpAccessTokenGenerator();
        var tokens = Enumerable.Range(0, 100).Select(_ => generator.Create().AccessToken).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(100, tokens.Count);
        Assert.All(tokens, token => Assert.StartsWith("mcp_", token, StringComparison.Ordinal));
    }

    private static McpAccessTokenValidator CreateValidator(FakeTokenRepository repository) =>
        new(repository, new FakeUserRepository(), new FakePasswordHasher(), new FakeClock(Now));

    private static McpAccessToken CreateToken(DateTimeOffset? expiresAtUtc = null)
    {
        var expires = expiresAtUtc ?? Now.AddHours(1);
        var created = expires <= Now ? expires.AddMinutes(-1) : Now.AddMinutes(-1);

        return new(
            Guid.NewGuid(),
            UserId,
            "client",
            "identifier",
            "hash",
            created,
            expires,
            [new(McpAccessTokenScopeCatalog.NotesRead), new(McpAccessTokenScopeCatalog.SubjectsRead)]);
    }

    private sealed class FakeTokenRepository(McpAccessToken token) : IMcpAccessTokenRepository
    {
        public List<McpAccessToken> Updated { get; } = [];
        public Task<McpAccessToken?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<McpAccessToken?>(null);
        public Task<McpAccessToken?> FindByTokenIdentifierAsync(string tokenIdentifier, CancellationToken ct) => Task.FromResult<McpAccessToken?>(tokenIdentifier == token.TokenIdentifier ? token : null);
        public Task<IReadOnlyCollection<McpAccessToken>> ListByUserAsync(Guid userId, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<McpAccessToken>>([]);
        public Task AddAsync(McpAccessToken token, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> UpdateAsync(McpAccessToken token, CancellationToken ct) { Updated.Add(token); return Task.FromResult(true); }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<User?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<User?>(id == UserId ? new User { Id = UserId, Login = "user" } : null);
        public Task<User?> FindAsync(string normalizedLogin, CancellationToken ct) => Task.FromResult<User?>(null);
        public Task AddAsync(User user, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "hash";
        public bool Verify(string password, string encoded) => password == "secret" && encoded == "hash";
    }

    private sealed class FakeClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
