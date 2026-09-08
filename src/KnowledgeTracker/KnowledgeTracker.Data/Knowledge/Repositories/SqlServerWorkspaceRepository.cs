using System.Data;
using System.Data.Common;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Data.Knowledge.Repositories;

// Workspace queries take UserId explicitly so ownership is enforced at the
// persistence boundary rather than trusted from a route or request body.
public sealed class SqlServerWorkspaceRepository(Func<DbConnection> connectionFactory) : IWorkspaceRepository
{
    public async Task<IReadOnlyCollection<Workspace>> ListAsync(Guid userId, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, UserId, Name, CreatedAtUtc FROM dbo.Workspaces WHERE UserId = @UserId ORDER BY CreatedAtUtc, Id;";
        command.AddParameter("@UserId", DbType.Guid, userId);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var workspaces = new List<Workspace>();
        while (await reader.ReadAsync(ct))
            workspaces.Add(ReadWorkspace(reader));
        return workspaces;
    }

    public async Task<Workspace?> FindAsync(Guid id, Guid userId, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, UserId, Name, CreatedAtUtc FROM dbo.Workspaces WHERE Id = @Id AND UserId = @UserId;";
        command.AddParameter("@Id", DbType.Guid, id);
        command.AddParameter("@UserId", DbType.Guid, userId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadWorkspace(reader) : null;
    }

    public async Task<int> CountAsync(Guid userId, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM dbo.Workspaces WHERE UserId = @UserId;";
        command.AddParameter("@UserId", DbType.Guid, userId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }

    public async Task AddAsync(Workspace workspace, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO dbo.Workspaces (Id, UserId, Name, CreatedAtUtc) VALUES (@Id, @UserId, @Name, @CreatedAtUtc);";
        command.AddParameter("@Id", DbType.Guid, workspace.Id);
        command.AddParameter("@UserId", DbType.Guid, workspace.UserId);
        command.AddParameter("@Name", DbType.String, workspace.Name);
        command.AddParameter("@CreatedAtUtc", DbType.DateTimeOffset, workspace.CreatedAtUtc);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static Workspace ReadWorkspace(DbDataReader reader) =>
        new(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetFieldValue<DateTimeOffset>(3));
}
