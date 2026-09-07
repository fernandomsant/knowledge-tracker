using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Infrastructure.Authentication.Services.AccessTokens;

public sealed class McpAccessTokenValidator(
    IMcpAccessTokenRepository tokens,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IClock clock
) : IMcpAccessTokenValidator
{
    public async Task<McpAccessTokenValidationResult?> ValidateAsync(string accessToken, CancellationToken ct)
    {
        if (!McpAccessTokenParser.TryParse(accessToken, out var identifier, out var secret))
            return null;

        var token = await tokens.FindByTokenIdentifierAsync(identifier, ct);
        if (token is null || !passwordHasher.Verify(secret, token.SecretHash) || !token.IsActive(clock.UtcNow))
            return null;

        var user = await users.FindByIdAsync(token.UserId, ct);
        if (user is null)
            return null;

        var delegatedScopes = token.Scopes.Select(scope => scope.Value).ToArray();
        token.MarkUsed(clock.UtcNow);
        await tokens.UpdateAsync(token, ct);
        return new McpAccessTokenValidationResult(user.Id, token.Id, delegatedScopes);
    }

    private static class McpAccessTokenParser
    {
        public static bool TryParse(string value, out string identifier, out string secret)
        {
            identifier = string.Empty;
            secret = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var parts = value.Split('_', StringSplitOptions.None);
            if (parts.Length != 3 || !string.Equals(parts[0], "mcp", StringComparison.Ordinal))
                return false;
            if (string.IsNullOrWhiteSpace(parts[1]) || string.IsNullOrWhiteSpace(parts[2]))
                return false;

            identifier = parts[1];
            secret = parts[2];
            return true;
        }
    }
}
