using KnowledgeTracker.Application.Authentication;

namespace KnowledgeTracker.Web.Authentication.Contracts;

public sealed record AuthenticationResponse(
    string AccessToken,
    string TokenType,
    long ExpiresIn
)
{
    public static AuthenticationResponse From(TokenPair tokens)
    {
        var expiresIn = Math.Max(
            0,
            (long)(tokens.AccessToken.Claims.ExpiresAtUtc - DateTimeOffset.UtcNow).TotalSeconds
        );
        return new AuthenticationResponse(
            tokens.AccessToken.Value,
            "Bearer",
            expiresIn
        );
    }
}
