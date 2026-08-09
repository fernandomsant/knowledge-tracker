using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Application.Authentication;

public interface ISessionRepository
{
    Task CreateWithSessionLimitAsync(
        AuthenticationSession session,
        RefreshTokenHash refreshTokenHash,
        RefreshTokenMetadata refreshTokenMetadata,
        int maximumActiveSessions,
        CancellationToken ct
    );

    Task<RefreshRotationResult> RotateAsync(
        RefreshTokenHash currentRefreshTokenHash,
        RefreshTokenHash nextRefreshTokenHash,
        TimeSpan refreshTokenLifetime,
        CancellationToken ct
    );

    Task RevokeAsync(Guid sessionId, CancellationToken ct);
}