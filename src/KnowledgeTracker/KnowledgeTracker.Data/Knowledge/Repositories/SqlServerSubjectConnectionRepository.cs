using System.Data;
using System.Data.Common;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Data.Knowledge.Repositories;

public sealed class SqlServerSubjectConnectionRepository(Func<DbConnection> connectionFactory)
    : ISubjectConnectionRepository
{
    public async Task<SubjectConnection?> FindAsync(Guid id, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, SubjectId, ConnectedSubjectId
            FROM dbo.SubjectConnections
            WHERE Id = @Id;
            """;
        command.AddParameter("@Id", DbType.Guid, id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadConnection(reader) : null;
    }

    public async Task<bool> ExistsAsync(
        Guid subjectId,
        Guid connectedSubjectId,
        CancellationToken ct
    )
    {
        var connection = new SubjectConnection(subjectId, connectedSubjectId);
        await using var databaseConnection = connectionFactory();
        await databaseConnection.OpenAsync(ct);
        await using var command = databaseConnection.CreateCommand();
        command.CommandText = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.SubjectConnections
                WHERE SubjectId = @SubjectId
                  AND ConnectedSubjectId = @ConnectedSubjectId
            ) THEN 1 ELSE 0 END;
            """;
        command.AddParameter("@SubjectId", DbType.Guid, connection.SubjectId);
        command.AddParameter("@ConnectedSubjectId", DbType.Guid, connection.ConnectedSubjectId);
        return Convert.ToBoolean(await command.ExecuteScalarAsync(ct));
    }

    public async Task<IReadOnlyCollection<SubjectConnection>> ListBySubjectAsync(
        Guid subjectId,
        CancellationToken ct
    )
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, SubjectId, ConnectedSubjectId
            FROM dbo.SubjectConnections
            WHERE SubjectId = @SubjectId OR ConnectedSubjectId = @SubjectId
            ORDER BY Id;
            """;
        command.AddParameter("@SubjectId", DbType.Guid, subjectId);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var connections = new List<SubjectConnection>();
        while (await reader.ReadAsync(ct))
            connections.Add(ReadConnection(reader));
        return connections;
    }

    public async Task AddAsync(SubjectConnection connection, CancellationToken ct)
    {
        await using var databaseConnection = connectionFactory();
        await databaseConnection.OpenAsync(ct);
        await using var command = databaseConnection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.SubjectConnections (Id, SubjectId, ConnectedSubjectId)
            VALUES (@Id, @SubjectId, @ConnectedSubjectId);
            """;
        command.AddParameter("@Id", DbType.Guid, connection.Id);
        command.AddParameter("@SubjectId", DbType.Guid, connection.SubjectId);
        command.AddParameter("@ConnectedSubjectId", DbType.Guid, connection.ConnectedSubjectId);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dbo.SubjectConnections WHERE Id = @Id;";
        command.AddParameter("@Id", DbType.Guid, id);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static SubjectConnection ReadConnection(DbDataReader reader) =>
        new(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2));
}
