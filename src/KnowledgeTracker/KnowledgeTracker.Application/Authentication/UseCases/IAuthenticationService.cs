using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Application.Authentication;

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