using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Domain.Authentication;
using Xunit;

namespace KnowledgeTracker.Tests.Authentication;

public sealed class McpAccessTokenServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_ReturnsOneTimeTokenAndPersistsNormalizedScopes()
    {
        var repository = new FakeTokenRepository();
        var generator = new FakeTokenGenerator(new("identifier", "raw-secret", "mcp_identifier_raw-secret"));
        var service = CreateService(repository: repository, generator: generator);

        var result = await service.CreateAsync(
            new(
                " Desktop client ",
                Now.AddDays(30),
                ["NOTES:READ", "notes:read", "subjects:write"]
            ),
            CancellationToken.None
        );

        var stored = Assert.Single(repository.Added);
        Assert.Equal("mcp_identifier_raw-secret", result.AccessToken);
        Assert.Equal("Desktop client", result.Name);
        Assert.Equal(["notes:read", "subjects:write"], result.Scopes);
        Assert.Equal(["notes:read", "subjects:write"], stored.Scopes.Select(scope => scope.Value));
        Assert.Equal("encoded-hash", stored.SecretHash);
        Assert.DoesNotContain("raw-secret", stored.SecretHash);
        Assert.Equal(UserId, stored.UserId);
        Assert.Equal(1, generator.CreateCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsUnknownScope()
    {
        var generator = new FakeTokenGenerator(new("identifier", "raw-secret", "token"));
        var repository = new FakeTokenRepository();
        var service = CreateService(repository: repository, generator: generator);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            new("client", Now.AddDays(30), ["users:delete"]),
            CancellationToken.None
        ));

        Assert.Empty(repository.Added);
        Assert.Equal(0, generator.CreateCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsExpiredToken()
    {
        var generator = new FakeTokenGenerator(new("identifier", "raw-secret", "token"));
        var service = CreateService(generator: generator);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateAsync(
            new("client", Now, [McpAccessTokenScopeCatalog.NotesRead]),
            CancellationToken.None
        ));

        Assert.Equal(0, generator.CreateCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsEmptyScopes()
    {
        var repository = new FakeTokenRepository();
        var service = CreateService(repository: repository);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            new("client", Now.AddDays(30), []),
            CancellationToken.None));

        Assert.Empty(repository.Added);
    }

    [Fact]
    public async Task CreateAsync_RequiresAuthenticatedUser()
    {
        var service = CreateService(currentUser: new FakeCurrentUserContext(null));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateAsync(
            new("client", Now.AddDays(30), [McpAccessTokenScopeCatalog.NotesRead]),
            CancellationToken.None
        ));
    }

    [Fact]
    public async Task ListAsync_ReturnsMetadataWithoutSecretMaterial()
    {
        var repository = new FakeTokenRepository();
        var owned = CreateToken(UserId);
        repository.Stored.Add(owned);
        repository.Stored.Add(CreateToken(Guid.Parse("22222222-2222-2222-2222-222222222222")));
        var service = CreateService(repository: repository);

        var result = await service.ListAsync(CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(owned.Id, dto.Id);
        Assert.Equal(owned.Name, dto.Name);
        Assert.Equal([McpAccessTokenScopeCatalog.NotesRead], dto.Scopes);
        Assert.DoesNotContain("SecretHash", string.Join(',', dto.GetType().GetProperties().Select(property => property.Name)));
    }

    [Fact]
    public async Task ListAsync_RequiresAuthenticatedUser()
    {
        var service = CreateService(currentUser: new FakeCurrentUserContext(null));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ListAsync(CancellationToken.None));
    }

    [Fact]
    public async Task RevokeAsync_RevokesOwnedToken()
    {
        var repository = new FakeTokenRepository();
        var token = CreateToken(UserId);
        repository.Stored.Add(token);
        var service = CreateService(repository: repository);

        var result = await service.RevokeAsync(token.Id, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(Now, token.RevokedAtUtc);
        Assert.Same(token, Assert.Single(repository.Updated));
    }

    [Fact]
    public async Task RevokeAsync_RejectsTokenOwnedByAnotherUser()
    {
        var repository = new FakeTokenRepository();
        var token = CreateToken(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        repository.Stored.Add(token);
        var service = CreateService(repository: repository);

        var result = await service.RevokeAsync(token.Id, CancellationToken.None);

        Assert.False(result);
        Assert.Null(token.RevokedAtUtc);
        Assert.Empty(repository.Updated);
    }

    [Fact]
    public async Task RevokeAsync_RequiresAuthenticatedUser()
    {
        var service = CreateService(currentUser: new FakeCurrentUserContext(null));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RevokeAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task TokenManagement_RequiresExplicitScopeForMcpCaller()
    {
        var currentUser = new FakeCurrentUserContext(UserId, true, McpAccessTokenScopeCatalog.NotesRead);
        var service = CreateService(
            currentUser: currentUser,
            authorization: new McpAwareActionAuthorizationService(currentUser));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ListAsync(CancellationToken.None));
    }

    private static McpAccessTokenService CreateService(
        FakeTokenRepository? repository = null,
        FakeTokenGenerator? generator = null,
        FakeCurrentUserContext? currentUser = null,
        IActionAuthorizationService? authorization = null
    ) => new(
        repository ?? new FakeTokenRepository(),
        generator ?? new FakeTokenGenerator(new("identifier", "raw-secret", "token")),
        new FakePasswordHasher(),
        currentUser ?? new FakeCurrentUserContext(UserId),
        new FakeClock(Now),
        authorization
    );

    private static McpAccessToken CreateToken(Guid userId) => new(
        Guid.NewGuid(),
        userId,
        "client",
        "identifier",
        "stored-hash",
        Now.AddDays(-1),
        Now.AddDays(30),
        [new(McpAccessTokenScopeCatalog.NotesRead)]
    );

    private sealed class FakeCurrentUserContext(Guid? userId, bool isMcpAccessToken = false, params string[] scopes) : ICurrentUserContext
    {
        public Guid? UserId { get; } = userId;
        public bool IsMcpAccessToken { get; } = isMcpAccessToken;
        public IReadOnlySet<string> Scopes { get; } = scopes.ToHashSet(StringComparer.Ordinal);
    }

    private sealed class FakeTokenGenerator(McpAccessTokenMaterial material) : IMcpAccessTokenGenerator
    {
        public int CreateCount { get; private set; }

        public McpAccessTokenMaterial Create()
        {
            CreateCount++;
            return material;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string value) => "encoded-hash";

        public bool Verify(string value, string encoded) => encoded == "encoded-hash";
    }

    private sealed class FakeClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FakeTokenRepository : IMcpAccessTokenRepository
    {
        public List<McpAccessToken> Added { get; } = [];
        public List<McpAccessToken> Stored { get; } = [];
        public List<McpAccessToken> Updated { get; } = [];

        public Task AddAsync(McpAccessToken token, CancellationToken ct)
        {
            Added.Add(token);
            Stored.Add(token);
            return Task.CompletedTask;
        }

        public Task<McpAccessToken?> FindByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Stored.SingleOrDefault(token => token.Id == id));

        public Task<McpAccessToken?> FindByTokenIdentifierAsync(string tokenIdentifier, CancellationToken ct) =>
            Task.FromResult(Stored.SingleOrDefault(token => token.TokenIdentifier == tokenIdentifier));

        public Task<IReadOnlyCollection<McpAccessToken>> ListByUserAsync(Guid userId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<McpAccessToken>>(Stored.Where(token => token.UserId == userId).ToArray());

        public Task<bool> UpdateAsync(McpAccessToken token, CancellationToken ct)
        {
            Updated.Add(token);
            return Task.FromResult(true);
        }
    }
}
