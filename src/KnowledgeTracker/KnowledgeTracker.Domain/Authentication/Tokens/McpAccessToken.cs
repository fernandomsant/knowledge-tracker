namespace KnowledgeTracker.Domain.Authentication;

public sealed class McpAccessToken
{
    private readonly IReadOnlyCollection<McpAccessTokenScope> scopes;

    public McpAccessToken(
        Guid id,
        Guid userId,
        string name,
        string tokenIdentifier,
        string secretHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        IEnumerable<McpAccessTokenScope> scopes,
        DateTimeOffset? revokedAtUtc = null,
        DateTimeOffset? lastUsedAtUtc = null
    )
    {
        if (id == Guid.Empty)
            throw new ArgumentException("MCP access-token identifier is required.", nameof(id));
        if (userId == Guid.Empty)
            throw new ArgumentException("MCP access-token user identifier is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 256)
            throw new ArgumentException("MCP access-token name is required and must be 256 characters or fewer.", nameof(name));
        if (string.IsNullOrWhiteSpace(tokenIdentifier) || tokenIdentifier.Length > 128)
            throw new ArgumentException("MCP access-token identifier is required and must be 128 characters or fewer.", nameof(tokenIdentifier));
        if (tokenIdentifier.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_')))
            throw new ArgumentException("MCP access-token identifier contains invalid characters.", nameof(tokenIdentifier));
        if (string.IsNullOrWhiteSpace(secretHash))
            throw new ArgumentException("MCP access-token secret hash is required.", nameof(secretHash));
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));
        if (expiresAtUtc <= createdAtUtc)
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc), "MCP access-token expiration must be after creation.");
        EnsureOptionalUtc(revokedAtUtc, nameof(revokedAtUtc));
        EnsureOptionalUtc(lastUsedAtUtc, nameof(lastUsedAtUtc));
        if (revokedAtUtc < createdAtUtc)
            throw new ArgumentOutOfRangeException(nameof(revokedAtUtc));
        if (lastUsedAtUtc < createdAtUtc)
            throw new ArgumentOutOfRangeException(nameof(lastUsedAtUtc));

        var normalizedScopes = McpAccessTokenScopeCatalog.Normalize(
            scopes?.Select(scope => scope?.Value ?? throw new ArgumentException("MCP access-token scope cannot be null.", nameof(scopes)))
                ?? throw new ArgumentNullException(nameof(scopes))
        );
        if (normalizedScopes.Count == 0)
            throw new ArgumentException("At least one MCP access-token scope is required.", nameof(scopes));

        Id = id;
        UserId = userId;
        Name = name.Trim();
        TokenIdentifier = tokenIdentifier;
        SecretHash = secretHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        this.scopes = normalizedScopes;
        RevokedAtUtc = revokedAtUtc;
        LastUsedAtUtc = lastUsedAtUtc;
    }

    public Guid Id { get; }
    public Guid UserId { get; }
    public string Name { get; }
    public string TokenIdentifier { get; }
    public string SecretHash { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public DateTimeOffset? LastUsedAtUtc { get; private set; }
    public IReadOnlyCollection<McpAccessTokenScope> Scopes => scopes;

    public bool IsActive(DateTimeOffset nowUtc)
    {
        EnsureUtc(nowUtc, nameof(nowUtc));
        return RevokedAtUtc is null && ExpiresAtUtc > nowUtc;
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        EnsureUtc(revokedAtUtc, nameof(revokedAtUtc));
        if (revokedAtUtc < CreatedAtUtc)
            throw new ArgumentOutOfRangeException(nameof(revokedAtUtc));

        RevokedAtUtc ??= revokedAtUtc;
    }

    public void MarkUsed(DateTimeOffset lastUsedAtUtc)
    {
        EnsureUtc(lastUsedAtUtc, nameof(lastUsedAtUtc));
        if (lastUsedAtUtc < CreatedAtUtc)
            throw new ArgumentOutOfRangeException(nameof(lastUsedAtUtc));

        LastUsedAtUtc = lastUsedAtUtc;
    }

    private static void EnsureOptionalUtc(DateTimeOffset? value, string parameterName)
    {
        if (value is not null)
            EnsureUtc(value.Value, parameterName);
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new ArgumentException("The timestamp must be UTC.", parameterName);
    }
}
