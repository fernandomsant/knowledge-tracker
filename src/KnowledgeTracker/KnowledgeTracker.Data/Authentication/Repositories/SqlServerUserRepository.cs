using System.Data;
using System.Data.Common;
using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Data.Authentication.Repositories;

public sealed class SqlServerUserRepository(Func<DbConnection> connectionFactory) : IUserRepository
{
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

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return new User
        {
            Id = reader.GetGuid(0),
            Login = reader.GetString(1),
            PasswordHash = reader.GetString(2),
        };
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
}
