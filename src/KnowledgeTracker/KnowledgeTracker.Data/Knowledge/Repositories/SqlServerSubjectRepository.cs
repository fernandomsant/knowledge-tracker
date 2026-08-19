using System.Data;
using System.Data.Common;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Data.Knowledge.Repositories;

public sealed class SqlServerSubjectRepository(Func<DbConnection> connectionFactory) : ISubjectRepository
{
    public async Task<Subject?> FindAsync(Guid id, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = CreateFindCommand(connection, id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadSubject(reader) : null;
    }

    public async Task<IReadOnlyCollection<Subject>> ListAsync(CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Description, ParentSubjectId FROM dbo.Subjects ORDER BY Name, Id;";
        await using var reader = await command.ExecuteReaderAsync(ct);

        var subjects = new List<Subject>();
        while (await reader.ReadAsync(ct))
            subjects.Add(ReadSubject(reader));
        return subjects;
    }

    public async Task<bool> HasChildrenAsync(Guid subjectId, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Subjects WHERE ParentSubjectId = @SubjectId) THEN 1 ELSE 0 END;";
        command.AddParameter("@SubjectId", DbType.Guid, subjectId);
        return Convert.ToBoolean(await command.ExecuteScalarAsync(ct));
    }

    public async Task AddAsync(Subject subject, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.Subjects (Id, Name, Description, ParentSubjectId)
            VALUES (@Id, @Name, @Description, @ParentSubjectId);
            """;
        AddSubjectParameters(command, subject);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateAsync(Subject subject, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.Subjects
            SET Name = @Name, Description = @Description, ParentSubjectId = @ParentSubjectId
            WHERE Id = @Id;
            """;
        AddSubjectParameters(command, subject);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        await using (var removeConnections = connection.CreateCommand())
        {
            removeConnections.Transaction = transaction;
            removeConnections.CommandText = """
                DELETE FROM dbo.SubjectConnections
                WHERE SubjectId = @Id OR ConnectedSubjectId = @Id;
                """;
            removeConnections.AddParameter("@Id", DbType.Guid, id);
            await removeConnections.ExecuteNonQueryAsync(ct);
        }

        await using (var promoteChildren = connection.CreateCommand())
        {
            promoteChildren.Transaction = transaction;
            promoteChildren.CommandText = "UPDATE dbo.Subjects SET ParentSubjectId = NULL WHERE ParentSubjectId = @Id;";
            promoteChildren.AddParameter("@Id", DbType.Guid, id);
            await promoteChildren.ExecuteNonQueryAsync(ct);
        }

        await using (var removeSubject = connection.CreateCommand())
        {
            removeSubject.Transaction = transaction;
            removeSubject.CommandText = "DELETE FROM dbo.Subjects WHERE Id = @Id;";
            removeSubject.AddParameter("@Id", DbType.Guid, id);
            await removeSubject.ExecuteNonQueryAsync(ct);
        }

        await transaction.CommitAsync(ct);
    }

    private static DbCommand CreateFindCommand(DbConnection connection, Guid id)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Description, ParentSubjectId FROM dbo.Subjects WHERE Id = @Id;";
        command.AddParameter("@Id", DbType.Guid, id);
        return command;
    }

    private static void AddSubjectParameters(DbCommand command, Subject subject)
    {
        command.AddParameter("@Id", DbType.Guid, subject.Id);
        command.AddParameter("@Name", DbType.String, subject.Name);
        command.AddParameter("@Description", DbType.String, (object?)subject.Description ?? DBNull.Value);
        command.AddParameter("@ParentSubjectId", DbType.Guid, (object?)subject.ParentSubjectId ?? DBNull.Value);
    }

    private static Subject ReadSubject(DbDataReader reader) =>
        new(reader.GetGuid(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetGuid(3));
}
