using System.Security.Cryptography;
using KnowledgeTracker.Application.Authentication;

namespace KnowledgeTracker.Infrastructure.Authentication;

public sealed class OpaqueMcpAccessTokenGenerator : IMcpAccessTokenGenerator
{
    public McpAccessTokenMaterial Create()
    {
        var tokenIdentifier = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var secret = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return new McpAccessTokenMaterial(tokenIdentifier, secret, $"mcp_{tokenIdentifier}_{secret}");
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
