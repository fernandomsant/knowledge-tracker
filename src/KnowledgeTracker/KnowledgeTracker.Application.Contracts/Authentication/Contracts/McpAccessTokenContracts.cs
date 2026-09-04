namespace KnowledgeTracker.Application.Authentication;

public sealed record CreateMcpAccessTokenRequest(
    string Name,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyCollection<string> Scopes
);

public sealed record CreateMcpAccessTokenResult(
    Guid Id,
    string Name,
    string AccessToken,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyCollection<string> Scopes
);

public sealed record McpAccessTokenDto(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc,
    DateTimeOffset? LastUsedAtUtc,
    IReadOnlyCollection<string> Scopes
);
