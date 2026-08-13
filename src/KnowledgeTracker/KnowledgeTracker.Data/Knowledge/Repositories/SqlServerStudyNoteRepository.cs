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
            SELECT note.Id, note.SubjectId, note.TopicId, note.Title, note.Content, note.StudyDurationTicks, note.StudyStartedAtUtc,
                   definition.Id, definition.Name, definition.NumberKind, metric.MetricValue
            FROM dbo.StudyNotes AS note
            LEFT JOIN dbo.StudyNoteMetrics AS metric ON metric.StudyNoteId = note.Id
            LEFT JOIN dbo.StudyMetricDefinitions AS definition ON definition.Id = metric.MetricDefinitionId
            WHERE note.Id = @Id;
            """;
        command.AddParameter("@Id", DbType.Guid, id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return (await ReadStudyNotesAsync(reader, ct)).SingleOrDefault();
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
            SELECT note.Id, note.SubjectId, note.TopicId, note.Title, note.Content, note.StudyDurationTicks, note.StudyStartedAtUtc,
                   definition.Id, definition.Name, definition.NumberKind, metric.MetricValue
            FROM dbo.StudyNotes AS note
            LEFT JOIN dbo.StudyNoteMetrics AS metric ON metric.StudyNoteId = note.Id
            LEFT JOIN dbo.StudyMetricDefinitions AS definition ON definition.Id = metric.MetricDefinitionId
            WHERE note.SubjectId = @SubjectId
            ORDER BY note.StudyStartedAtUtc DESC, note.Id, definition.NormalizedName;
            """;
        command.AddParameter("@SubjectId", DbType.Guid, subjectId);
        await using var reader = await command.ExecuteReaderAsync(ct);

        return await ReadStudyNotesAsync(reader, ct);
    }

    public async Task AddAsync(StudyNote studyNote, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO dbo.StudyNotes
                (Id, SubjectId, TopicId, Title, Content, StudyDurationTicks, StudyStartedAtUtc)
            VALUES
                (@Id, @SubjectId, @TopicId, @Title, @Content, @StudyDurationTicks, @StudyStartedAtUtc);
            """;
        AddStudyNoteParameters(command, studyNote);
        await command.ExecuteNonQueryAsync(ct);
        await InsertMetricsAsync(connection, transaction, studyNote, ct);
        await transaction.CommitAsync(ct);
    }

    public async Task UpdateAsync(StudyNote studyNote, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE dbo.StudyNotes
            SET Title = @Title,
                TopicId = @TopicId,
                Content = @Content,
                StudyDurationTicks = @StudyDurationTicks,
                StudyStartedAtUtc = @StudyStartedAtUtc
            WHERE Id = @Id;
            """;
        AddStudyNoteParameters(command, studyNote);
        await command.ExecuteNonQueryAsync(ct);
        command.Parameters.Clear();
        command.CommandText = "DELETE FROM dbo.StudyNoteMetrics WHERE StudyNoteId = @Id;";
        command.AddParameter("@Id", DbType.Guid, studyNote.Id);
        await command.ExecuteNonQueryAsync(ct);
        await InsertMetricsAsync(connection, transaction, studyNote, ct);
        await transaction.CommitAsync(ct);
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
        command.AddParameter("@TopicId", DbType.Guid, studyNote.TopicId);
        command.AddParameter("@Title", DbType.String, studyNote.Title);
        command.AddParameter("@Content", DbType.String, studyNote.Content);
        command.AddParameter("@StudyDurationTicks", DbType.Int64, studyNote.StudyDuration.Ticks);
        command.AddParameter("@StudyStartedAtUtc", DbType.DateTimeOffset, studyNote.StudyStartedAtUtc);
    }

    private static async Task<IReadOnlyCollection<StudyNote>> ReadStudyNotesAsync(
        DbDataReader reader,
        CancellationToken ct
    )
    {
        var rows = new Dictionary<Guid, StudyNoteRow>();
        while (await reader.ReadAsync(ct))
        {
            var id = reader.GetGuid(0);
            if (!rows.TryGetValue(id, out var row))
            {
                row = new StudyNoteRow(
                    id,
                    reader.GetGuid(1),
                    reader.GetGuid(2), reader.GetString(3), reader.GetString(4),
                    TimeSpan.FromTicks(reader.GetInt64(5)), reader.GetFieldValue<DateTimeOffset>(6)
                );
                rows.Add(id, row);
            }

            if (!reader.IsDBNull(7))
                row.Metrics.Add(new StudyNoteMetric(
                    new StudyMetricDefinition(reader.GetGuid(7), reader.GetString(8), (MetricNumberKind)reader.GetByte(9)), reader.GetDecimal(10)
                ));
        }

        return rows.Values.Select(row => new StudyNote(
            row.Id, row.SubjectId, row.TopicId, row.Title, row.Content, row.StudyDuration, row.StudyStartedAtUtc, row.Metrics
        )).ToArray();
    }

    private static async Task InsertMetricsAsync(
        DbConnection connection,
        DbTransaction transaction,
        StudyNote studyNote,
        CancellationToken ct
    )
    {
        foreach (var metric in studyNote.Metrics)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO dbo.StudyNoteMetrics (StudyNoteId, MetricDefinitionId, MetricValue)
                VALUES (@StudyNoteId, @MetricDefinitionId, @MetricValue);
                """;
            command.AddParameter("@StudyNoteId", DbType.Guid, studyNote.Id);
            command.AddParameter("@MetricDefinitionId", DbType.Guid, metric.Definition.Id);
            command.AddParameter("@MetricValue", DbType.Decimal, metric.Value);
            await command.ExecuteNonQueryAsync(ct);
        }
    }

    private sealed class StudyNoteRow(
        Guid id,
        Guid subjectId,
        Guid topicId,
        string title,
        string content,
        TimeSpan studyDuration,
        DateTimeOffset studyStartedAtUtc
    )
    {
        public Guid Id { get; } = id;
        public Guid SubjectId { get; } = subjectId;
        public Guid TopicId { get; } = topicId;
        public string Title { get; } = title;
        public string Content { get; } = content;
        public TimeSpan StudyDuration { get; } = studyDuration;
        public DateTimeOffset StudyStartedAtUtc { get; } = studyStartedAtUtc;
        public List<StudyNoteMetric> Metrics { get; } = [];
    }
}
