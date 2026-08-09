namespace KnowledgeTracker.Domain.Authentication;

public sealed class AuthenticationSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public Guid Nonce { get; init; } = Guid.NewGuid();
    public DateTimeOffset AuthenticatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAtUtc { get; init; } = DateTimeOffset.UtcNow.AddHours(24);
    public string UserAgent { get; init; } = "unknown";
    public string ClientIpAddress { get; init; } = "0.0.0.0";
    public int ClientSourcePort { get; init; }
    public bool Revoked { get; private set; }

    public bool TryRotate()
    {
        if (Revoked || ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            Revoke();
            return false;
        }

        return true;
    }

    public void Revoke() => Revoked = true;
}