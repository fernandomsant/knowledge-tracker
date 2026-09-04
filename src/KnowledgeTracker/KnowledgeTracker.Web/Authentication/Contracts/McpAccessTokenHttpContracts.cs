using System.ComponentModel.DataAnnotations;

namespace KnowledgeTracker.Web.Authentication.Contracts;

public sealed record CreateMcpAccessTokenHttpRequest
{
    /// <summary>The human-readable name shown when managing the token.</summary>
    [Required, StringLength(200, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>The UTC instant at which the token stops being valid.</summary>
    [Required]
    public DateTimeOffset ExpiresAtUtc { get; init; }

    /// <summary>The MCP actions that the client is allowed to perform.</summary>
    [Required, MinLength(1)]
    public IReadOnlyCollection<string> Scopes { get; init; } = [];
}

public sealed record McpAccessTokenHttpResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc,
    DateTimeOffset? LastUsedAtUtc,
    IReadOnlyCollection<string> Scopes
);

public sealed record CreateMcpAccessTokenHttpResponse(
    Guid Id,
    string Name,
    string AccessToken,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyCollection<string> Scopes
);
