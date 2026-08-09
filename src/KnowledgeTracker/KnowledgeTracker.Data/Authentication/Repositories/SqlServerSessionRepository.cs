using System.Data;
using System.Data.Common;
using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Data.Authentication.Repositories;

public sealed class SqlServerSessionRepository(Func<DbConnection> connectionFactory) : ISessionRepository
{
    public async Task CreateWithSessionLimitAsync(
        AuthenticationSession session,
        RefreshTokenHash refreshTokenHash,
        RefreshTokenMetadata refreshTokenMetadata,
        int maximumActiveSessions,
        CancellationToken ct
    )
    {
        if (maximumActiveSessions < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumActiveSessions));
        if (refreshTokenMetadata.SessionId != session.Id)
            throw new ArgumentException("Refresh-token metadata does not belong to the session.");

        var now = DateTimeOffset.UtcNow;
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var activeSessionCount = await CountActiveSessionsAsync(connection, transaction, session.UserId, now, ct);
        if (activeSessionCount >= maximumActiveSessions)
        {
            await RevokeOldestSessionsAsync(
                connection,
                transaction,
                session.UserId,
                now,
                activeSessionCount - maximumActiveSessions + 1,
                ct
            );
        }

        await InsertSessionAsync(connection, transaction, session, ct);
        await InsertRefreshTokenAsync(
            connection,
            transaction,
            refreshTokenHash,
            refreshTokenMetadata,
            ct
        );
        await transaction.CommitAsync(ct);
    }

    public async Task<RefreshRotationResult> RotateAsync(
        RefreshTokenHash currentRefreshTokenHash,
        RefreshTokenHash nextRefreshTokenHash,
        TimeSpan refreshTokenLifetime,
        CancellationToken ct
    )
    {
        var now = DateTimeOffset.UtcNow;
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var storedToken = await FindRefreshTokenAsync(
            connection,
            transaction,
            currentRefreshTokenHash,
            ct
        );

        if (storedToken is null)
        {
            await transaction.CommitAsync(ct);
            return new RefreshRotationResult(RotationOutcome.NotFound, null);
        }

        if (storedToken.ConsumedAtUtc is not null)
        {
            await RevokeSessionAsync(connection, transaction, storedToken.Session.Id, ct);
            await transaction.CommitAsync(ct);
            return new RefreshRotationResult(RotationOutcome.ReplayDetected, null);
        }

        if (
            storedToken.Session.Revoked
            || storedToken.Session.ExpiresAtUtc <= now
            || storedToken.Claims.ExpiresAtUtc <= now
        )
        {
            await transaction.CommitAsync(ct);
            return new RefreshRotationResult(RotationOutcome.Expired, null);
        }

        await ConsumeRefreshTokenAsync(connection, transaction, currentRefreshTokenHash, now, ct);
        var nextExpiry = storedToken.Session.ExpiresAtUtc < now.Add(refreshTokenLifetime)
            ? storedToken.Session.ExpiresAtUtc
            : now.Add(refreshTokenLifetime);
        await InsertRefreshTokenAsync(
            connection,
            transaction,
            nextRefreshTokenHash,
            new RefreshTokenMetadata(
                storedToken.Session.Id,
                new TokenClaims(
                    storedToken.Claims.Issuer,
                    storedToken.Claims.Subject,
                    storedToken.Claims.AuthenticatedAtUtc,
                    nextExpiry,
                    storedToken.Claims.Nonce
                )
            ),
            ct
        );
        await transaction.CommitAsync(ct);
        return new RefreshRotationResult(RotationOutcome.Succeeded, storedToken.Session);
    }

    public async Task RevokeAsync(Guid sessionId, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.AuthenticationSessions SET IsRevoked = 1 WHERE Id = @SessionId;";
        command.AddParameter("@SessionId", DbType.Guid, sessionId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<long> CountActiveSessionsAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct
    )
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT COUNT_BIG(*)
            FROM dbo.AuthenticationSessions WITH (UPDLOCK, HOLDLOCK)
            WHERE UserId = @UserId
              AND IsRevoked = 0
              AND ExpiresAtUtc > @Now;
            """;
        command.AddParameter("@UserId", DbType.Guid, userId);
        command.AddParameter("@Now", DbType.DateTimeOffset, now);
        return Convert.ToInt64(await command.ExecuteScalarAsync(ct));
    }

    private static async Task RevokeOldestSessionsAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid userId,
        DateTimeOffset now,
        long sessionsToRevoke,
        CancellationToken ct
    )
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            ;WITH SessionsToRevoke AS
            (
                SELECT TOP (@SessionsToRevoke) Id
                FROM dbo.AuthenticationSessions WITH (UPDLOCK, HOLDLOCK)
                WHERE UserId = @UserId
                  AND IsRevoked = 0
                  AND ExpiresAtUtc > @Now
                ORDER BY AuthenticatedAtUtc, Id
            )
            UPDATE dbo.AuthenticationSessions
            SET IsRevoked = 1
            WHERE Id IN (SELECT Id FROM SessionsToRevoke);
            """;
        command.AddParameter("@SessionsToRevoke", DbType.Int64, sessionsToRevoke);
        command.AddParameter("@UserId", DbType.Guid, userId);
        command.AddParameter("@Now", DbType.DateTimeOffset, now);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertSessionAsync(
        DbConnection connection,
        DbTransaction transaction,
        AuthenticationSession session,
        CancellationToken ct
    )
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO dbo.AuthenticationSessions
                (Id, UserId, Nonce, AuthenticatedAtUtc, ExpiresAtUtc, UserAgent, ClientIpAddress, ClientSourcePort)
            VALUES
                (@Id, @UserId, @Nonce, @AuthenticatedAtUtc, @ExpiresAtUtc, @UserAgent, @ClientIpAddress, @ClientSourcePort);
            """;
        command.AddParameter("@Id", DbType.Guid, session.Id);
        command.AddParameter("@UserId", DbType.Guid, session.UserId);
        command.AddParameter("@Nonce", DbType.Guid, session.Nonce);
        command.AddParameter("@AuthenticatedAtUtc", DbType.DateTimeOffset, session.AuthenticatedAtUtc);
        command.AddParameter("@ExpiresAtUtc", DbType.DateTimeOffset, session.ExpiresAtUtc);
        command.AddParameter("@UserAgent", DbType.String, session.UserAgent);
        command.AddParameter("@ClientIpAddress", DbType.AnsiString, session.ClientIpAddress);
        command.AddParameter("@ClientSourcePort", DbType.Int32, session.ClientSourcePort);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertRefreshTokenAsync(
        DbConnection connection,
        DbTransaction transaction,
        RefreshTokenHash refreshTokenHash,
        RefreshTokenMetadata refreshTokenMetadata,
        CancellationToken ct
    )
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO dbo.RefreshTokens
                (TokenHash, SessionId, Issuer, SubjectUserId, AuthenticatedAtUtc, ExpiresAtUtc, Nonce)
            VALUES
                (@TokenHash, @SessionId, @Issuer, @SubjectUserId, @AuthenticatedAtUtc, @ExpiresAtUtc, @Nonce);
            """;
        command.AddParameter("@TokenHash", DbType.Binary, refreshTokenHash.Value);
        command.AddParameter("@SessionId", DbType.Guid, refreshTokenMetadata.SessionId);
        command.AddParameter("@Issuer", DbType.String, refreshTokenMetadata.Claims.Issuer);
        command.AddParameter("@SubjectUserId", DbType.Guid, refreshTokenMetadata.Claims.Subject);
        command.AddParameter("@AuthenticatedAtUtc", DbType.DateTimeOffset, refreshTokenMetadata.Claims.AuthenticatedAtUtc);
        command.AddParameter("@ExpiresAtUtc", DbType.DateTimeOffset, refreshTokenMetadata.Claims.ExpiresAtUtc);
        command.AddParameter("@Nonce", DbType.Guid, refreshTokenMetadata.Claims.Nonce);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<StoredRefreshToken?> FindRefreshTokenAsync(
        DbConnection connection,
        DbTransaction transaction,
        RefreshTokenHash refreshTokenHash,
        CancellationToken ct
    )
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT
                s.Id,
                s.UserId,
                s.Nonce,
                s.AuthenticatedAtUtc,
                s.ExpiresAtUtc,
                s.UserAgent,
                s.ClientIpAddress,
                s.ClientSourcePort,
                s.IsRevoked,
                t.Issuer,
                t.SubjectUserId,
                t.AuthenticatedAtUtc,
                t.ExpiresAtUtc,
                t.Nonce,
                t.ConsumedAtUtc
            FROM dbo.RefreshTokens AS t WITH (UPDLOCK, HOLDLOCK)
            INNER JOIN dbo.AuthenticationSessions AS s WITH (UPDLOCK, HOLDLOCK) ON s.Id = t.SessionId
            WHERE t.TokenHash = @TokenHash;
            """;
        command.AddParameter("@TokenHash", DbType.Binary, refreshTokenHash.Value);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        var session = new AuthenticationSession
        {
            Id = reader.GetGuid(0),
            UserId = reader.GetGuid(1),
            Nonce = reader.GetGuid(2),
            AuthenticatedAtUtc = reader.GetFieldValue<DateTimeOffset>(3),
            ExpiresAtUtc = reader.GetFieldValue<DateTimeOffset>(4),
            UserAgent = reader.GetString(5),
            ClientIpAddress = reader.GetString(6),
            ClientSourcePort = reader.GetInt32(7),
        };
        if (reader.GetBoolean(8))
            session.Revoke();

        return new StoredRefreshToken(
            session,
            new TokenClaims(
                reader.GetString(9),
                reader.GetGuid(10),
                reader.GetFieldValue<DateTimeOffset>(11),
                reader.GetFieldValue<DateTimeOffset>(12),
                reader.GetGuid(13)
            ),
            reader.IsDBNull(14) ? null : reader.GetFieldValue<DateTimeOffset>(14)
        );
    }

    private static async Task ConsumeRefreshTokenAsync(
        DbConnection connection,
        DbTransaction transaction,
        RefreshTokenHash refreshTokenHash,
        DateTimeOffset now,
        CancellationToken ct
    )
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE dbo.RefreshTokens
            SET ConsumedAtUtc = @ConsumedAtUtc
            WHERE TokenHash = @TokenHash
              AND ConsumedAtUtc IS NULL;
            """;
        command.AddParameter("@ConsumedAtUtc", DbType.DateTimeOffset, now);
        command.AddParameter("@TokenHash", DbType.Binary, refreshTokenHash.Value);
        if (await command.ExecuteNonQueryAsync(ct) != 1)
            throw new InvalidOperationException("Refresh-token consumption lost its transaction lock.");
    }

    private static async Task RevokeSessionAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid sessionId,
        CancellationToken ct
    )
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE dbo.AuthenticationSessions SET IsRevoked = 1 WHERE Id = @SessionId;";
        command.AddParameter("@SessionId", DbType.Guid, sessionId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private sealed record StoredRefreshToken(
        AuthenticationSession Session,
        TokenClaims Claims,
        DateTimeOffset? ConsumedAtUtc
    );
}
