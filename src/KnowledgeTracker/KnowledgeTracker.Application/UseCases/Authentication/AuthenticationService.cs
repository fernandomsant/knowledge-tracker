using KnowledgeTracker.Domain.Authentication;
namespace KnowledgeTracker.Application.Authentication;
public sealed class AuthenticationService(IUserRepository users,ISessionRepository sessions,IPasswordHasher passwords,IAccessTokenService access,IRefreshTokenService refresh,AuthenticationOptions options):IAuthenticationService
{
 public async Task<TokenPair?> AuthenticateAsync(string name,string password,RequestContext context,CancellationToken ct)
 { var user=await users.FindAsync(name.Trim().ToUpperInvariant(),ct); if(user is null||!passwords.Verify(password,user.PasswordHash))return null; var now=DateTimeOffset.UtcNow; var session=new AuthenticationSession{UserId=user.Id,AuthenticatedAtUtc=now,ExpiresAtUtc=now.Add(options.SessionLifetime),UserAgent=context.UserAgent,ClientIpAddress=context.ClientIpAddress,ClientSourcePort=context.ClientSourcePort}; var token=refresh.Create();var refreshExpiry=Min(session.ExpiresAtUtc,now.Add(options.RefreshLifetime));await sessions.CreateAsync(session,token.Hash,refreshExpiry,ct);return new(access.Issue(session),token.Value,now.Add(options.AccessLifetime),refreshExpiry); }
 public async Task<TokenPair?> RefreshAsync(string value,CancellationToken ct)
 { var now=DateTimeOffset.UtcNow; var next=refresh.Create();var result=await sessions.RotateAsync(refresh.Hash(value),next.Hash,now.Add(options.RefreshLifetime),ct);if(result.Outcome!=RotationOutcome.Succeeded||result.Session is null)return null;var expiry=Min(result.Session.ExpiresAtUtc,now.Add(options.RefreshLifetime));return new(access.Issue(result.Session),next.Value,now.Add(options.AccessLifetime),expiry); }
 static DateTimeOffset Min(DateTimeOffset a,DateTimeOffset b)=>a<b?a:b;
}
