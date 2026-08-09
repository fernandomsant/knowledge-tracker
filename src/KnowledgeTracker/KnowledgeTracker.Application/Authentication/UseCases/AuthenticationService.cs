using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Application.Authentication;

public sealed class AuthenticationService(
    IUserRepository users,
    ISessionRepository sessions,
    IPasswordHasher passwords,
    IAccessTokenService accessTokens,
    IRefreshTokenService refreshTokens,
    AuthenticationOptions options
) : IAuthenticationService
{
    public async Task RegisterAsync(string login, string password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(login))
            throw new ArgumentException("Login is required.", nameof(login));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required.", nameof(password));

        var user = new User { Login = login.Trim(), PasswordHash = passwords.Hash(password) };
        if (await users.FindAsync(user.NormalizedLogin, ct) is not null)
            throw new InvalidOperationException("Login is already registered.");

        await users.AddAsync(user, ct);
    }

    public async Task<TokenPair?> AuthenticateAsync(
        string login,
        string password,
        RequestContext context,
        CancellationToken ct
    )
    {
        var user = await users.FindAsync(login.Trim().ToUpperInvariant(), ct);
        if (user is null || !passwords.Verify(password, user.PasswordHash))
            return null;

        var now = DateTimeOffset.UtcNow;
        var session = new AuthenticationSession
        {
            UserId = user.Id,
            AuthenticatedAtUtc = now,
            ExpiresAtUtc = now.Add(options.SessionLifetime),
            UserAgent = context.UserAgent,
            ClientIpAddress = context.ClientIpAddress,
            ClientSourcePort = context.ClientSourcePort,
        };
        var refreshToken = refreshTokens.Create();
        var refreshClaims = CreateClaims(session, Min(session.ExpiresAtUtc, now.Add(options.RefreshLifetime)));
        await sessions.CreateWithSessionLimitAsync(
            session,
            refreshTokens.Hash(refreshToken),
            RefreshTokenMetadata.For(session, refreshClaims),
            options.MaximumSessions,
            ct
        );

        return new TokenPair(IssueAccessToken(session, now), refreshToken);
    }

    public async Task<TokenPair?> RefreshAsync(RefreshToken refreshToken, CancellationToken ct)
    {
        var currentRefreshTokenHash = refreshTokens.TryHash(refreshToken);
        if (currentRefreshTokenHash is null)
            return null;

        var now = DateTimeOffset.UtcNow;
        var nextRefreshToken = refreshTokens.Create();
        var result = await sessions.RotateAsync(
            currentRefreshTokenHash,
            refreshTokens.Hash(nextRefreshToken),
            options.RefreshLifetime,
            ct
        );
        if (result.Outcome != RotationOutcome.Succeeded || result.Session is null)
            return null;

        return new TokenPair(IssueAccessToken(result.Session, now), nextRefreshToken);
    }

    private AccessToken IssueAccessToken(AuthenticationSession session, DateTimeOffset now) =>
        accessTokens.Issue(
            AccessToken.Unsigned(
                CreateClaims(session, Min(session.ExpiresAtUtc, now.Add(options.AccessLifetime))),
                session.Id,
                options.Audience
            )
        );

    private TokenClaims CreateClaims(AuthenticationSession session, DateTimeOffset expiresAtUtc) =>
        new(options.Issuer, session.UserId, session.AuthenticatedAtUtc, expiresAtUtc, session.Nonce);

    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;
}
