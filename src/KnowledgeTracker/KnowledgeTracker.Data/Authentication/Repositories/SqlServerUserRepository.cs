using System.Data;
using System.Data.Common;
using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Data.Authentication.Repositories;

public sealed class SqlServerUserRepository(Func<DbConnection> connectionFactory) : IUserRepository
{
    public async Task<User?> FindByIdAsync(Guid id, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Login, PasswordHash
            FROM dbo.Users
            WHERE Id = @Id;
            """;
        command.AddParameter("@Id", DbType.Guid, id);
        return await ReadUserAsync(command, ct);
    }

    public async Task<User?> FindAsync(string normalizedLogin, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Login, PasswordHash
            FROM dbo.Users
            WHERE NormalizedLogin = @NormalizedLogin;
            """;
        command.AddParameter("@NormalizedLogin", DbType.String, normalizedLogin);

        return await ReadUserAsync(command, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.Users (Id, Login, NormalizedLogin, PasswordHash)
            VALUES (@Id, @Login, @NormalizedLogin, @PasswordHash);
            """;
        command.AddParameter("@Id", DbType.Guid, user.Id);
        command.AddParameter("@Login", DbType.String, user.Login);
        command.AddParameter("@NormalizedLogin", DbType.String, user.NormalizedLogin);
        command.AddParameter("@PasswordHash", DbType.String, user.PasswordHash);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<User?> ReadUserAsync(DbCommand command, CancellationToken ct)
    {
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct)
            ? new User
            {
                Id = reader.GetGuid(0),
                Login = reader.GetString(1),
                PasswordHash = reader.GetString(2),
            }
            : null;
    }
}
