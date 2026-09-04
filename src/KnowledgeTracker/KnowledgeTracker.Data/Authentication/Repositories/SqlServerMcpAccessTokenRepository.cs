using System.Data;
using System.Data.Common;
using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Data.Authentication.Repositories;

public sealed class SqlServerMcpAccessTokenRepository(Func<DbConnection> connectionFactory)
    : IMcpAccessTokenRepository
{
    public async Task<McpAccessToken?> FindByIdAsync(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("MCP access-token identifier is required.", nameof(id));

        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = SelectSql + " WHERE token.Id = @Id ORDER BY scope.Scope;";
        command.AddParameter("@Id", DbType.Guid, id);
        return (await ReadAsync(command, ct)).SingleOrDefault();
    }

    public async Task<McpAccessToken?> FindByTokenIdentifierAsync(string tokenIdentifier, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenIdentifier);

        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = SelectSql + " WHERE token.TokenIdentifier = @TokenIdentifier ORDER BY scope.Scope;";
        command.AddParameter("@TokenIdentifier", DbType.AnsiString, tokenIdentifier);
        return (await ReadAsync(command, ct)).SingleOrDefault();
    }

    public async Task<IReadOnlyCollection<McpAccessToken>> ListByUserAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User identifier is required.", nameof(userId));

        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = SelectSql + " WHERE token.UserId = @UserId ORDER BY token.CreatedAtUtc DESC, token.Id, scope.Scope;";
        command.AddParameter("@UserId", DbType.Guid, userId);
        return await ReadAsync(command, ct);
    }

    public async Task AddAsync(McpAccessToken token, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(token);

        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        await using (var tokenCommand = connection.CreateCommand())
        {
            tokenCommand.Transaction = transaction;
            tokenCommand.CommandText = """
                INSERT INTO dbo.McpAccessTokens
                    (Id, UserId, Name, TokenIdentifier, SecretHash, CreatedAtUtc, ExpiresAtUtc, RevokedAtUtc, LastUsedAtUtc)
                VALUES
                    (@Id, @UserId, @Name, @TokenIdentifier, @SecretHash, @CreatedAtUtc, @ExpiresAtUtc, @RevokedAtUtc, @LastUsedAtUtc);
                """;
            AddTokenParameters(tokenCommand, token);
            await tokenCommand.ExecuteNonQueryAsync(ct);
        }

        foreach (var scope in token.Scopes)
        {
            await using var scopeCommand = connection.CreateCommand();
            scopeCommand.Transaction = transaction;
            scopeCommand.CommandText = """
                INSERT INTO dbo.McpAccessTokenScopes (McpAccessTokenId, Scope)
                VALUES (@McpAccessTokenId, @Scope);
                """;
            scopeCommand.AddParameter("@McpAccessTokenId", DbType.Guid, token.Id);
            scopeCommand.AddParameter("@Scope", DbType.AnsiString, scope.Value);
            await scopeCommand.ExecuteNonQueryAsync(ct);
        }

        await transaction.CommitAsync(ct);
    }

    public async Task<bool> UpdateAsync(McpAccessToken token, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(token);

        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.McpAccessTokens
            SET RevokedAtUtc = @RevokedAtUtc,
                LastUsedAtUtc = @LastUsedAtUtc
            WHERE Id = @Id AND UserId = @UserId;
            """;
        command.AddParameter("@RevokedAtUtc", DbType.DateTimeOffset, (object?)token.RevokedAtUtc ?? DBNull.Value);
        command.AddParameter("@LastUsedAtUtc", DbType.DateTimeOffset, (object?)token.LastUsedAtUtc ?? DBNull.Value);
        command.AddParameter("@Id", DbType.Guid, token.Id);
        command.AddParameter("@UserId", DbType.Guid, token.UserId);
        return await command.ExecuteNonQueryAsync(ct) == 1;
    }

    private const string SelectSql = """
        SELECT
            token.Id,
            token.UserId,
            token.Name,
            token.TokenIdentifier,
            token.SecretHash,
            token.CreatedAtUtc,
            token.ExpiresAtUtc,
            token.RevokedAtUtc,
            token.LastUsedAtUtc,
            scope.Scope
        FROM dbo.McpAccessTokens AS token
        LEFT JOIN dbo.McpAccessTokenScopes AS scope
            ON scope.McpAccessTokenId = token.Id
        """;

    private static async Task<IReadOnlyCollection<McpAccessToken>> ReadAsync(DbCommand command, CancellationToken ct)
    {
        await using var reader = await command.ExecuteReaderAsync(ct);
        var rows = new List<TokenRow>();
        while (await reader.ReadAsync(ct))
        {
            rows.Add(
                new TokenRow(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetFieldValue<DateTimeOffset>(5),
                    reader.GetFieldValue<DateTimeOffset>(6),
                    reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
                    reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
                    reader.IsDBNull(9) ? null : reader.GetString(9)
                )
            );
        }

        return rows
            .GroupBy(row => row.Id)
            .Select(group =>
                new McpAccessToken(
                    group.Key,
                    group.First().UserId,
                    group.First().Name,
                    group.First().TokenIdentifier,
                    group.First().SecretHash,
                    group.First().CreatedAtUtc,
                    group.First().ExpiresAtUtc,
                    group.Where(row => row.Scope is not null).Select(row => new McpAccessTokenScope(row.Scope!)),
                    group.First().RevokedAtUtc,
                    group.First().LastUsedAtUtc
                )
            )
            .ToArray();
    }

    private static void AddTokenParameters(DbCommand command, McpAccessToken token)
    {
        command.AddParameter("@Id", DbType.Guid, token.Id);
        command.AddParameter("@UserId", DbType.Guid, token.UserId);
        command.AddParameter("@Name", DbType.String, token.Name);
        command.AddParameter("@TokenIdentifier", DbType.AnsiString, token.TokenIdentifier);
        command.AddParameter("@SecretHash", DbType.String, token.SecretHash);
        command.AddParameter("@CreatedAtUtc", DbType.DateTimeOffset, token.CreatedAtUtc);
        command.AddParameter("@ExpiresAtUtc", DbType.DateTimeOffset, token.ExpiresAtUtc);
        command.AddParameter("@RevokedAtUtc", DbType.DateTimeOffset, (object?)token.RevokedAtUtc ?? DBNull.Value);
        command.AddParameter("@LastUsedAtUtc", DbType.DateTimeOffset, (object?)token.LastUsedAtUtc ?? DBNull.Value);
    }

    private sealed record TokenRow(
        Guid Id,
        Guid UserId,
        string Name,
        string TokenIdentifier,
        string SecretHash,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset? RevokedAtUtc,
        DateTimeOffset? LastUsedAtUtc,
        string? Scope
    );
}
