using System.Data;
using System.Data.Common;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Data.Knowledge.Repositories;

public sealed class SqlServerSubjectRepository(Func<DbConnection> connectionFactory, CurrentWorkspaceDataScope dataScope) : ISubjectRepository
{
    public async Task<Subject?> FindAsync(Guid id, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = CreateFindCommand(connection, id, dataScope.RequireUserId(), dataScope.RequireWorkspaceId());
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadSubject(reader) : null;
    }

    public async Task<IReadOnlyCollection<Subject>> ListAsync(CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, UserId, WorkspaceId, Name, Description, ParentSubjectId FROM dbo.Subjects WHERE UserId = @UserId AND WorkspaceId = @WorkspaceId ORDER BY Name, Id;";
        command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId());
        command.AddParameter("@WorkspaceId", DbType.Guid, dataScope.RequireWorkspaceId());
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
        command.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Subjects WHERE ParentSubjectId = @SubjectId AND UserId = @UserId AND WorkspaceId = @WorkspaceId) THEN 1 ELSE 0 END;";
        command.AddParameter("@SubjectId", DbType.Guid, subjectId);
        command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId());
        command.AddParameter("@WorkspaceId", DbType.Guid, dataScope.RequireWorkspaceId());
        return Convert.ToBoolean(await command.ExecuteScalarAsync(ct));
    }

    public async Task AddAsync(Subject subject, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.Subjects (Id, UserId, WorkspaceId, Name, Description, ParentSubjectId)
            VALUES (@Id, @UserId, @WorkspaceId, @Name, @Description, @ParentSubjectId);
            """;
        AddSubjectParameters(command, subject);
        command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId());
        command.AddParameter("@WorkspaceId", DbType.Guid, dataScope.RequireWorkspaceId());
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
            WHERE Id = @Id AND UserId = @UserId AND WorkspaceId = @WorkspaceId;
            """;
        AddSubjectParameters(command, subject);
        command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId());
        command.AddParameter("@WorkspaceId", DbType.Guid, dataScope.RequireWorkspaceId());
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
                WHERE (SubjectId = @Id OR ConnectedSubjectId = @Id)
                  AND EXISTS (SELECT 1 FROM dbo.Subjects WHERE Id = @Id AND UserId = @UserId AND WorkspaceId = @WorkspaceId);
                """;
            removeConnections.AddParameter("@Id", DbType.Guid, id);
            removeConnections.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId());
            removeConnections.AddParameter("@WorkspaceId", DbType.Guid, dataScope.RequireWorkspaceId());
            await removeConnections.ExecuteNonQueryAsync(ct);
        }

        await using (var promoteChildren = connection.CreateCommand())
        {
            promoteChildren.Transaction = transaction;
            promoteChildren.CommandText = "UPDATE dbo.Subjects SET ParentSubjectId = NULL WHERE ParentSubjectId = @Id AND UserId = @UserId AND WorkspaceId = @WorkspaceId;";
            promoteChildren.AddParameter("@Id", DbType.Guid, id);
            promoteChildren.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId());
            promoteChildren.AddParameter("@WorkspaceId", DbType.Guid, dataScope.RequireWorkspaceId());
            await promoteChildren.ExecuteNonQueryAsync(ct);
        }

        await using (var removeSubject = connection.CreateCommand())
        {
            removeSubject.Transaction = transaction;
            removeSubject.CommandText = "DELETE FROM dbo.Subjects WHERE Id = @Id AND UserId = @UserId AND WorkspaceId = @WorkspaceId;";
            removeSubject.AddParameter("@Id", DbType.Guid, id);
            removeSubject.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId());
            removeSubject.AddParameter("@WorkspaceId", DbType.Guid, dataScope.RequireWorkspaceId());
            await removeSubject.ExecuteNonQueryAsync(ct);
        }

        await transaction.CommitAsync(ct);
    }

    private static DbCommand CreateFindCommand(DbConnection connection, Guid id, Guid userId, Guid workspaceId)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, UserId, WorkspaceId, Name, Description, ParentSubjectId FROM dbo.Subjects WHERE Id = @Id AND UserId = @UserId AND WorkspaceId = @WorkspaceId;";
        command.AddParameter("@Id", DbType.Guid, id);
        command.AddParameter("@UserId", DbType.Guid, userId);
        command.AddParameter("@WorkspaceId", DbType.Guid, workspaceId);
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
        new(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetGuid(5));
}
