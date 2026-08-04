namespace KnowledgeTracker.Domain.Authentication;

public sealed record AccessToken(TokenClaims Claims, Guid SessionId, string Audience, string Value)
{
    public static AccessToken Unsigned(TokenClaims claims, Guid sessionId, string audience) =>
        new(claims, sessionId, audience, string.Empty);

    public AccessToken WithValue(string value) => this with { Value = value };
}