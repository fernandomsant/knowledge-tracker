using System.Collections.Concurrent;
using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Tests.Authentication.Fakes;

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> users = new();

    public Task<User?> FindAsync(string normalizedLogin, CancellationToken ct) =>
        Task.FromResult(users.TryGetValue(normalizedLogin, out var user) ? user : null);

    public Task AddAsync(User user, CancellationToken ct)
    {
        if (!users.TryAdd(user.NormalizedLogin, user))
            throw new InvalidOperationException("Duplicate user.");
        return Task.CompletedTask;
    }
}

public sealed class InMemorySessionRepository : ISessionRepository
{
    private sealed record StoredRefreshToken(RefreshTokenMetadata Metadata);

    private readonly object gate = new();
    private readonly Dictionary<Guid, AuthenticationSession> sessions = new();
    private readonly Dictionary<string, StoredRefreshToken> activeTokens = new();
    private readonly Dictionary<string, Guid> consumedTokens = new();

    public Task CreateWithSessionLimitAsync(
        AuthenticationSession session,
        RefreshTokenHash refreshTokenHash,
        RefreshTokenMetadata refreshTokenMetadata,
        int maximumActiveSessions,
        CancellationToken ct
    )
    {
        if (refreshTokenMetadata.SessionId != session.Id)
            throw new ArgumentException("Refresh-token metadata does not belong to the session.");
        if (maximumActiveSessions < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumActiveSessions));

        lock (gate)
        {
            var now = DateTimeOffset.UtcNow;
            var activeSessions = sessions.Values
                .Where(candidate =>
                    candidate.UserId == session.UserId
                    && !candidate.Revoked
                    && candidate.ExpiresAtUtc > now
                )
                .OrderBy(candidate => candidate.AuthenticatedAtUtc)
                .ToList();

            while (activeSessions.Count >= maximumActiveSessions)
            {
                activeSessions[0].Revoke();
                activeSessions.RemoveAt(0);
            }

            sessions[session.Id] = session;
            activeTokens[Key(refreshTokenHash)] = new(refreshTokenMetadata);
        }
        return Task.CompletedTask;
    }

    public Task<RefreshRotationResult> RotateAsync(
        RefreshTokenHash currentRefreshTokenHash,
        RefreshTokenHash nextRefreshTokenHash,
        TimeSpan refreshTokenLifetime,
        CancellationToken ct
    )
    {
        lock (gate)
        {
            var currentKey = Key(currentRefreshTokenHash);
            if (!activeTokens.Remove(currentKey, out var current))
            {
                if (consumedTokens.TryGetValue(currentKey, out var replaySessionId))
                    Revoke(replaySessionId);

                return Task.FromResult(
                    new RefreshRotationResult(
                        consumedTokens.ContainsKey(currentKey)
                            ? RotationOutcome.ReplayDetected
                            : RotationOutcome.NotFound,
                        null
                    )
                );
            }

            consumedTokens[currentKey] = current.Metadata.SessionId;
            if (!sessions.TryGetValue(current.Metadata.SessionId, out var session) || session.Revoked)
                return Task.FromResult(new RefreshRotationResult(RotationOutcome.Expired, null));
            if (
                session.ExpiresAtUtc <= DateTimeOffset.UtcNow
                || current.Metadata.Claims.ExpiresAtUtc <= DateTimeOffset.UtcNow
                || !session.TryRotate()
            )
                return Task.FromResult(new RefreshRotationResult(RotationOutcome.Expired, null));

            var expiry = session.ExpiresAtUtc < DateTimeOffset.UtcNow.Add(refreshTokenLifetime)
                ? session.ExpiresAtUtc
                : DateTimeOffset.UtcNow.Add(refreshTokenLifetime);
            var metadata = new RefreshTokenMetadata(
                session.Id,
                new TokenClaims(
                    current.Metadata.Claims.Issuer,
                    session.UserId,
                    session.AuthenticatedAtUtc,
                    expiry,
                    session.Nonce
                )
            );
            activeTokens[Key(nextRefreshTokenHash)] = new(metadata);
            return Task.FromResult(new RefreshRotationResult(RotationOutcome.Succeeded, session));
        }
    }

    public Task RevokeAsync(Guid sessionId, CancellationToken ct)
    {
        lock (gate)
            Revoke(sessionId);
        return Task.CompletedTask;
    }

    private void Revoke(Guid sessionId)
    {
        if (sessions.TryGetValue(sessionId, out var session))
            session.Revoke();
    }

    private static string Key(RefreshTokenHash hash) => Convert.ToHexString(hash.Value);
}