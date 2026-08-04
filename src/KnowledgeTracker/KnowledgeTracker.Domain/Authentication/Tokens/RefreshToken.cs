namespace KnowledgeTracker.Domain.Authentication;

public sealed record RefreshToken
{
    public RefreshToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Refresh token value is required.", nameof(value));

        Value = value;
    }

    public string Value { get; }
}

public sealed record RefreshTokenHash
{
    public RefreshTokenHash(byte[] value)
    {
        if (value.Length == 0)
            throw new ArgumentException("Refresh token hash is required.", nameof(value));

        Value = value.ToArray();
    }

    public byte[] Value { get; }
}

public sealed record RefreshTokenMetadata(Guid SessionId, TokenClaims Claims)
{
    public static RefreshTokenMetadata For(AuthenticationSession session, TokenClaims claims) =>
        new(session.Id, claims);
}