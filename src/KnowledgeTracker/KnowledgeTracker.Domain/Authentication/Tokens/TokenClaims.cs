namespace KnowledgeTracker.Domain.Authentication;

public sealed record TokenClaims
{
    public TokenClaims(
        string issuer,
        Guid subject,
        DateTimeOffset authenticatedAtUtc,
        DateTimeOffset expiresAtUtc,
        Guid nonce
    )
    {
        if (string.IsNullOrWhiteSpace(issuer))
            throw new ArgumentException("Token issuer is required.", nameof(issuer));
        if (subject == Guid.Empty)
            throw new ArgumentException("Token subject is required.", nameof(subject));
        if (nonce == Guid.Empty)
            throw new ArgumentException("Token nonce is required.", nameof(nonce));
        if (expiresAtUtc <= authenticatedAtUtc)
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc));

        Issuer = issuer;
        Subject = subject;
        AuthenticatedAtUtc = authenticatedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Nonce = nonce;
    }

    public string Issuer { get; }
    public Guid Subject { get; }
    public DateTimeOffset AuthenticatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public Guid Nonce { get; }
}