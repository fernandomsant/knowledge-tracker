using KnowledgeTracker.Domain.Authentication;
namespace KnowledgeTracker.Application.Authentication;
public sealed record AuthenticationOptions(string Issuer,string Audience,TimeSpan AccessLifetime,TimeSpan RefreshLifetime,TimeSpan SessionLifetime,int MaximumSessions){public static AuthenticationOptions Default=>new("knowledge-tracker","knowledge-tracker-api",TimeSpan.FromMinutes(15),TimeSpan.FromHours(5),TimeSpan.FromHours(24),5);}
public sealed record TokenPair(string AccessToken,string RefreshToken,DateTimeOffset AccessExpiresAtUtc,DateTimeOffset RefreshExpiresAtUtc);
public sealed record RequestContext(string UserAgent,string ClientIpAddress,int ClientSourcePort);
public sealed record RefreshTokenMaterial(string Value,byte[] Hash);
public enum RotationOutcome{Succeeded,NotFound,Expired,ReplayDetected}
public interface IUserRepository{Task<User?> FindAsync(string normalizedUserName,CancellationToken ct);Task AddAsync(User user,CancellationToken ct);}
public interface ISessionRepository{Task CreateAsync(AuthenticationSession session,byte[] refreshHash,DateTimeOffset refreshExpiry,CancellationToken ct);Task<(RotationOutcome Outcome,AuthenticationSession? Session)> RotateAsync(byte[] current,byte[] next,DateTimeOffset expiry,CancellationToken ct);Task RevokeAsync(Guid sessionId,CancellationToken ct);}
public interface IPasswordHasher{string Hash(string password);bool Verify(string password,string encoded);}
public interface IAccessTokenService{string Issue(AuthenticationSession session);}
public interface IRefreshTokenService{RefreshTokenMaterial Create();byte[] Hash(string value);}
public interface IAuthenticationService{Task<TokenPair?> AuthenticateAsync(string userName,string password,RequestContext context,CancellationToken ct);Task<TokenPair?> RefreshAsync(string refreshToken,CancellationToken ct);}
