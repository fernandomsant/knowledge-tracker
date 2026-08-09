using System.Data;
using System.Data.Common;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Data.Knowledge.Repositories;

public sealed class SqlServerStudyNoteRepository(Func<DbConnection> connectionFactory) : IStudyNoteRepository
{
    public async Task<StudyNote?> FindAsync(Guid id, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, SubjectId, Title, Content, StudyDurationTicks, StudyStartedAtUtc
            FROM dbo.StudyNotes
            WHERE Id = @Id;
            """;
        command.AddParameter("@Id", DbType.Guid, id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadStudyNote(reader) : null;
    }

    public async Task<IReadOnlyCollection<StudyNote>> ListBySubjectAsync(
        Guid subjectId,
        CancellationToken ct
    )
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, SubjectId, Title, Content, StudyDurationTicks, StudyStartedAtUtc
            FROM dbo.StudyNotes
            WHERE SubjectId = @SubjectId
            ORDER BY StudyStartedAtUtc DESC, Id;
            """;
        command.AddParameter("@SubjectId", DbType.Guid, subjectId);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var notes = new List<StudyNote>();
        while (await reader.ReadAsync(ct))
            notes.Add(ReadStudyNote(reader));
        return notes;
    }

    public async Task AddAsync(StudyNote studyNote, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.StudyNotes
                (Id, SubjectId, Title, Content, StudyDurationTicks, StudyStartedAtUtc)
            VALUES
                (@Id, @SubjectId, @Title, @Content, @StudyDurationTicks, @StudyStartedAtUtc);
            """;
        AddStudyNoteParameters(command, studyNote);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateAsync(StudyNote studyNote, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.StudyNotes
            SET Title = @Title,
                Content = @Content,
                StudyDurationTicks = @StudyDurationTicks
            WHERE Id = @Id;
            """;
        AddStudyNoteParameters(command, studyNote);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dbo.StudyNotes WHERE Id = @Id;";
        command.AddParameter("@Id", DbType.Guid, id);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static void AddStudyNoteParameters(DbCommand command, StudyNote studyNote)
    {
        command.AddParameter("@Id", DbType.Guid, studyNote.Id);
        command.AddParameter("@SubjectId", DbType.Guid, studyNote.SubjectId);
        command.AddParameter("@Title", DbType.String, studyNote.Title);
        command.AddParameter("@Content", DbType.String, studyNote.Content);
        command.AddParameter("@StudyDurationTicks", DbType.Int64, studyNote.StudyDuration.Ticks);
        command.AddParameter("@StudyStartedAtUtc", DbType.DateTimeOffset, studyNote.StudyStartedAtUtc);
    }

    private static StudyNote ReadStudyNote(DbDataReader reader) =>
        new(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetString(3),
            TimeSpan.FromTicks(reader.GetInt64(4)),
            reader.GetFieldValue<DateTimeOffset>(5)
        );
}
