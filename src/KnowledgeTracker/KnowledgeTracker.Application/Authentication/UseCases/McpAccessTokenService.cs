using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Application.Authentication;

public sealed class McpAccessTokenService(
    IMcpAccessTokenRepository tokens,
    IMcpAccessTokenGenerator tokenGenerator,
    IPasswordHasher passwordHasher,
    ICurrentUserContext currentUser,
    IClock clock,
    IActionAuthorizationService? authorization = null
) : IMcpAccessTokenService
{
    public async Task<CreateMcpAccessTokenResult> CreateAsync(
        CreateMcpAccessTokenRequest request,
        CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        authorization?.Demand(McpAccessTokenScopeCatalog.TokensManage);
        var userId = RequireUserId();
        var now = RequireUtc(clock.UtcNow, nameof(clock.UtcNow));
        var requestedScopes = McpAccessTokenScopeCatalog.Normalize(request.Scopes);

        if (request.ExpiresAtUtc <= now)
            throw new ArgumentOutOfRangeException(nameof(request.ExpiresAtUtc), "MCP access-token expiration must be in the future.");

        var material = tokenGenerator.Create();
        ArgumentNullException.ThrowIfNull(material);
        var token = new McpAccessToken(
            Guid.NewGuid(),
            userId,
            request.Name,
            material.TokenIdentifier,
            passwordHasher.Hash(material.Secret),
            now,
            request.ExpiresAtUtc,
            requestedScopes
        );
        await tokens.AddAsync(token, ct);

        return new CreateMcpAccessTokenResult(
            token.Id,
            token.Name,
            material.AccessToken,
            token.CreatedAtUtc,
            token.ExpiresAtUtc,
            token.Scopes.Select(scope => scope.Value).ToArray()
        );
    }

    public async Task<IReadOnlyCollection<McpAccessTokenDto>> ListAsync(CancellationToken ct)
    {
        authorization?.Demand(McpAccessTokenScopeCatalog.TokensManage);
        var userId = RequireUserId();
        var tokensForUser = await tokens.ListByUserAsync(userId, ct);
        return tokensForUser.Select(ToDto).ToArray();
    }

    public async Task<bool> RevokeAsync(Guid id, CancellationToken ct)
    {
        authorization?.Demand(McpAccessTokenScopeCatalog.TokensManage);
        if (id == Guid.Empty)
            throw new ArgumentException("MCP access-token identifier is required.", nameof(id));

        var userId = RequireUserId();
        var token = await tokens.FindByIdAsync(id, ct);
        if (token is null || token.UserId != userId)
            return false;

        token.Revoke(RequireUtc(clock.UtcNow, nameof(clock.UtcNow)));
        return await tokens.UpdateAsync(token, ct);
    }

    private Guid RequireUserId() => currentUser.UserId is { } userId && userId != Guid.Empty
        ? userId
        : throw new UnauthorizedAccessException("An authenticated user is required.");

    private static McpAccessTokenDto ToDto(McpAccessToken token) =>
        new(
            token.Id,
            token.Name,
            token.CreatedAtUtc,
            token.ExpiresAtUtc,
            token.RevokedAtUtc,
            token.LastUsedAtUtc,
            token.Scopes.Select(scope => scope.Value).ToArray()
        );

    private static DateTimeOffset RequireUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("The timestamp must be UTC.", parameterName);
        return value;
    }
}
