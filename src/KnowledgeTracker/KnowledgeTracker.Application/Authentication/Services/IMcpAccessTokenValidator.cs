namespace KnowledgeTracker.Application.Authentication;

public interface IMcpAccessTokenValidator
{
    Task<McpAccessTokenValidationResult?> ValidateAsync(string accessToken, CancellationToken ct);
}

public sealed record McpAccessTokenValidationResult(
    Guid UserId,
    Guid TokenId,
    IReadOnlyCollection<string> DelegatedScopes,
    IReadOnlyCollection<string> Scopes
);
