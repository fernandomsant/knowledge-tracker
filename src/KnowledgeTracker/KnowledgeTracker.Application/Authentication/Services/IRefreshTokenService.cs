using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Application.Authentication;

public interface IRefreshTokenService
{
    RefreshToken Create();
    RefreshTokenHash Hash(RefreshToken token);
    RefreshTokenHash? TryHash(RefreshToken token);
}