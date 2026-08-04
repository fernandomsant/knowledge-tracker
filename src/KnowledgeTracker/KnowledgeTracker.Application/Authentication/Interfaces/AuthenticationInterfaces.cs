using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Application.Authentication;

public interface IUserRepository
{
    Task<User?> FindAsync(string normalizedLogin, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
}

public interface ISessionRepository
{
    Task CreateAsync(
        AuthenticationSession session,
        RefreshToken refreshToken,
        RefreshTokenHash refreshTokenHash,
        RefreshTokenMetadata refreshTokenMetadata,
        CancellationToken ct
    );

    Task<RefreshRotationResult> RotateAsync(
        RefreshTokenHash currentRefreshTokenHash,
        RefreshToken nextRefreshToken,
        RefreshTokenHash nextRefreshTokenHash,
        TimeSpan refreshTokenLifetime,
        CancellationToken ct
    );

    Task RevokeAsync(Guid sessionId, CancellationToken ct);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string encoded);
}

public interface IAccessTokenService
{
    AccessToken Issue(AccessToken unsignedToken);
}

public interface IRefreshTokenService
{
    RefreshToken Create();
    RefreshTokenHash Hash(RefreshToken token);
}

public interface IAuthenticationService
{
    Task RegisterAsync(string login, string password, CancellationToken ct);
    Task<TokenPair?> AuthenticateAsync(
        string login,
        string password,
        RequestContext context,
        CancellationToken ct
    );
    Task<TokenPair?> RefreshAsync(RefreshToken refreshToken, CancellationToken ct);
}