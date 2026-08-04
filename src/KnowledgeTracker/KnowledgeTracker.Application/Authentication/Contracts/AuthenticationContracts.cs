using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Application.Authentication;

public sealed record AuthenticationOptions(
    string Issuer,
    string Audience,
    TimeSpan AccessLifetime,
    TimeSpan RefreshLifetime,
    TimeSpan SessionLifetime,
    int MaximumSessions
)
{
    public static AuthenticationOptions Default =>
        new(
            "knowledge-tracker",
            "knowledge-tracker-api",
            TimeSpan.FromMinutes(15),
            TimeSpan.FromHours(5),
            TimeSpan.FromHours(24),
            5
        );
}

public sealed record TokenPair(AccessToken AccessToken, RefreshToken RefreshToken);

public sealed record RequestContext(string UserAgent, string ClientIpAddress, int ClientSourcePort);

public sealed record RefreshRotationResult(RotationOutcome Outcome, AuthenticationSession? Session);

public enum RotationOutcome
{
    Succeeded,
    NotFound,
    Expired,
    ReplayDetected,
}