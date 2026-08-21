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
                   note.NoteVersion, job.Status, job.LastError, run.Model, run.ModelVersion,
                   classification.TopicId, classification.TopicName, classification.Score,
                   definition.Id, definition.Name, definition.NumberKind, metric.MetricValue
            FROM dbo.StudyNotes AS note
            OUTER APPLY
            (
                SELECT TOP (1) currentJob.Status, currentJob.LastError, currentJob.TaxonomyVersion
                FROM dbo.ClassificationJobs AS currentJob
                WHERE currentJob.NoteId = note.Id AND currentJob.NoteVersion = note.NoteVersion
                ORDER BY currentJob.TaxonomyVersion DESC, currentJob.CreatedAtUtc DESC
            ) AS job
            OUTER APPLY
            (
                SELECT TOP (1) currentRun.Id, currentRun.Model, currentRun.ModelVersion
                FROM dbo.ClassificationRuns AS currentRun
                WHERE currentRun.NoteId = note.Id AND currentRun.NoteVersion = note.NoteVersion
                  AND currentRun.TaxonomyVersion = job.TaxonomyVersion
                ORDER BY currentRun.TaxonomyVersion DESC, currentRun.CreatedAtUtc DESC
            ) AS run
            LEFT JOIN dbo.NoteClassifications AS classification ON classification.ClassificationRunId = run.Id
            LEFT JOIN dbo.StudyNoteMetrics AS metric ON metric.StudyNoteId = note.Id
            LEFT JOIN dbo.StudyMetricDefinitions AS definition ON definition.Id = metric.MetricDefinitionId
            WHERE note.Id = @Id;
            """;
        command.AddParameter("@Id", DbType.Guid, id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return (await ReadStudyNotesAsync(reader, ct)).SingleOrDefault();
    }

    public async Task<IReadOnlyCollection<StudyNote>> ListAsync(CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT note.Id, note.SubjectId, note.TopicId, note.Title, note.Content, note.StudyDurationTicks, note.StudyStartedAtUtc,
                   note.NoteVersion, job.Status, job.LastError, run.Model, run.ModelVersion,
                   classification.TopicId, classification.TopicName, classification.Score,
                   definition.Id, definition.Name, definition.NumberKind, metric.MetricValue
            FROM dbo.StudyNotes AS note
            OUTER APPLY
            (
                SELECT TOP (1) currentJob.Status, currentJob.LastError, currentJob.TaxonomyVersion
                FROM dbo.ClassificationJobs AS currentJob
                WHERE currentJob.NoteId = note.Id AND currentJob.NoteVersion = note.NoteVersion
                ORDER BY currentJob.TaxonomyVersion DESC, currentJob.CreatedAtUtc DESC
            ) AS job
            OUTER APPLY
            (
                SELECT TOP (1) currentRun.Id, currentRun.Model, currentRun.ModelVersion
                FROM dbo.ClassificationRuns AS currentRun
                WHERE currentRun.NoteId = note.Id AND currentRun.NoteVersion = note.NoteVersion
                  AND currentRun.TaxonomyVersion = job.TaxonomyVersion
                ORDER BY currentRun.TaxonomyVersion DESC, currentRun.CreatedAtUtc DESC
            ) AS run
            LEFT JOIN dbo.NoteClassifications AS classification ON classification.ClassificationRunId = run.Id
            LEFT JOIN dbo.StudyNoteMetrics AS metric ON metric.StudyNoteId = note.Id
            LEFT JOIN dbo.StudyMetricDefinitions AS definition ON definition.Id = metric.MetricDefinitionId
            ORDER BY note.StudyStartedAtUtc DESC, note.Id, definition.NormalizedName;
            """;
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await ReadStudyNotesAsync(reader, ct);
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
                   note.NoteVersion, job.Status, job.LastError, run.Model, run.ModelVersion,
                   classification.TopicId, classification.TopicName, classification.Score,
                   definition.Id, definition.Name, definition.NumberKind, metric.MetricValue
            FROM dbo.StudyNotes AS note
            OUTER APPLY
            (
                SELECT TOP (1) currentJob.Status, currentJob.LastError, currentJob.TaxonomyVersion
                FROM dbo.ClassificationJobs AS currentJob
                WHERE currentJob.NoteId = note.Id AND currentJob.NoteVersion = note.NoteVersion
                ORDER BY currentJob.TaxonomyVersion DESC, currentJob.CreatedAtUtc DESC
            ) AS job
            OUTER APPLY
            (
                SELECT TOP (1) currentRun.Id, currentRun.Model, currentRun.ModelVersion
                FROM dbo.ClassificationRuns AS currentRun
                WHERE currentRun.NoteId = note.Id AND currentRun.NoteVersion = note.NoteVersion
                  AND currentRun.TaxonomyVersion = job.TaxonomyVersion
                ORDER BY currentRun.TaxonomyVersion DESC, currentRun.CreatedAtUtc DESC
            ) AS run
            LEFT JOIN dbo.NoteClassifications AS classification ON classification.ClassificationRunId = run.Id
            LEFT JOIN dbo.StudyNoteMetrics AS metric ON metric.StudyNoteId = note.Id
            LEFT JOIN dbo.StudyMetricDefinitions AS definition ON definition.Id = metric.MetricDefinitionId
            WHERE note.SubjectId = @SubjectId
            ORDER BY note.StudyStartedAtUtc DESC, note.Id, definition.NormalizedName;
            """;
        command.AddParameter("@SubjectId", DbType.Guid, subjectId);
        await using var reader = await command.ExecuteReaderAsync(ct);

        return await ReadStudyNotesAsync(reader, ct);
    }

    public async Task<IReadOnlyCollection<StudyNote>> ListBySubjectTreeAsync(
        Guid subjectId,
        CancellationToken ct
    )
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            WITH DescendantSubjects AS
            (
                SELECT Id
                FROM dbo.Subjects
                WHERE Id = @SubjectId

                UNION ALL

                SELECT child.Id
                FROM dbo.Subjects AS child
                INNER JOIN DescendantSubjects AS ancestor ON ancestor.Id = child.ParentSubjectId
            )
            SELECT note.Id, note.SubjectId, note.TopicId, note.Title, note.Content, note.StudyDurationTicks, note.StudyStartedAtUtc,
                   note.NoteVersion, job.Status, job.LastError, run.Model, run.ModelVersion,
                   classification.TopicId, classification.TopicName, classification.Score,
                   definition.Id, definition.Name, definition.NumberKind, metric.MetricValue
            FROM dbo.StudyNotes AS note
            INNER JOIN DescendantSubjects AS subject ON subject.Id = note.SubjectId
            OUTER APPLY
            (
                SELECT TOP (1) currentJob.Status, currentJob.LastError, currentJob.TaxonomyVersion
                FROM dbo.ClassificationJobs AS currentJob
                WHERE currentJob.NoteId = note.Id AND currentJob.NoteVersion = note.NoteVersion
                ORDER BY currentJob.TaxonomyVersion DESC, currentJob.CreatedAtUtc DESC
            ) AS job
            OUTER APPLY
            (
                SELECT TOP (1) currentRun.Id, currentRun.Model, currentRun.ModelVersion
                FROM dbo.ClassificationRuns AS currentRun
                WHERE currentRun.NoteId = note.Id AND currentRun.NoteVersion = note.NoteVersion
                  AND currentRun.TaxonomyVersion = job.TaxonomyVersion
                ORDER BY currentRun.TaxonomyVersion DESC, currentRun.CreatedAtUtc DESC
            ) AS run
            LEFT JOIN dbo.NoteClassifications AS classification ON classification.ClassificationRunId = run.Id
            LEFT JOIN dbo.StudyNoteMetrics AS metric ON metric.StudyNoteId = note.Id
            LEFT JOIN dbo.StudyMetricDefinitions AS definition ON definition.Id = metric.MetricDefinitionId
            ORDER BY note.StudyStartedAtUtc DESC, note.Id, definition.NormalizedName
            OPTION (MAXRECURSION 4);
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
                (Id, SubjectId, TopicId, Title, Content, StudyDurationTicks, StudyStartedAtUtc, NoteVersion)
            VALUES
                (@Id, @SubjectId, @TopicId, @Title, @Content, @StudyDurationTicks, @StudyStartedAtUtc, @NoteVersion);
            """;
        AddStudyNoteParameters(command, studyNote);
        await command.ExecuteNonQueryAsync(ct);
        await InsertMetricsAsync(connection, transaction, studyNote, ct);
        await UpsertManualRelationAsync(connection, transaction, studyNote, ct);
        await EnqueueClassificationAsync(connection, transaction, studyNote, ct);
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
                StudyStartedAtUtc = @StudyStartedAtUtc,
                NoteVersion = @NoteVersion
            WHERE Id = @Id AND NoteVersion = @ExpectedNoteVersion;
            """;
        AddStudyNoteParameters(command, studyNote);
        command.AddParameter("@ExpectedNoteVersion", DbType.Int64, studyNote.Version - 1);
        if (await command.ExecuteNonQueryAsync(ct) != 1)
            throw new DBConcurrencyException("The study note was changed by another request.");
        command.Parameters.Clear();
        command.CommandText = "DELETE FROM dbo.StudyNoteMetrics WHERE StudyNoteId = @Id;";
        command.AddParameter("@Id", DbType.Guid, studyNote.Id);
        await command.ExecuteNonQueryAsync(ct);
        await InsertMetricsAsync(connection, transaction, studyNote, ct);
        await UpsertManualRelationAsync(connection, transaction, studyNote, ct);
        await EnqueueClassificationAsync(connection, transaction, studyNote, ct);
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
        command.AddParameter("@SubjectId", DbType.Guid, (object?)studyNote.SubjectId ?? DBNull.Value);
        command.AddParameter("@TopicId", DbType.Guid, (object?)studyNote.TopicId ?? DBNull.Value);
        command.AddParameter("@Title", DbType.String, studyNote.Title);
        command.AddParameter("@Content", DbType.String, studyNote.Content);
        command.AddParameter("@StudyDurationTicks", DbType.Int64, studyNote.StudyDuration.Ticks);
        command.AddParameter("@StudyStartedAtUtc", DbType.DateTimeOffset, studyNote.StudyStartedAtUtc);
        command.AddParameter("@NoteVersion", DbType.Int64, studyNote.Version);
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
                    reader.IsDBNull(1) ? null : reader.GetGuid(1),
                    reader.IsDBNull(2) ? null : reader.GetGuid(2), reader.GetString(3), reader.GetString(4),
                    TimeSpan.FromTicks(reader.GetInt64(5)), reader.GetFieldValue<DateTimeOffset>(6),
                    reader.GetInt64(7),
                    reader.IsDBNull(8) ? NoteClassificationStatus.Pending : (NoteClassificationStatus)reader.GetByte(8),
                    reader.IsDBNull(9) ? null : reader.GetString(9),
                    reader.IsDBNull(10) ? null : reader.GetString(10),
                    reader.IsDBNull(11) ? null : reader.GetString(11)
                );
                rows.Add(id, row);
            }

            if (!reader.IsDBNull(12))
                row.Classifications.TryAdd(reader.GetGuid(12), new NoteClassificationScore(
                    reader.GetGuid(12), reader.GetString(13), Convert.ToDouble(reader.GetDecimal(14))
                ));

            if (!reader.IsDBNull(15))
                row.Metrics.TryAdd(reader.GetGuid(15), new StudyNoteMetric(
                    new StudyMetricDefinition(reader.GetGuid(15), reader.GetString(16), (MetricNumberKind)reader.GetByte(17)), reader.GetDecimal(18)
                ));
        }

        return rows.Values.Select(row => new StudyNote(
            row.Id, row.SubjectId, row.TopicId, row.Title, row.Content, row.StudyDuration, row.StudyStartedAtUtc,
            row.Metrics.Values, row.Version,
            new NoteClassificationState(row.ClassificationStatus, row.Model, row.ModelVersion, row.FailureReason, row.Classifications.Values)
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

    private static async Task UpsertManualRelationAsync(
        DbConnection connection,
        DbTransaction transaction,
        StudyNote studyNote,
        CancellationToken ct
    )
    {
        if (studyNote.SubjectId is not Guid subjectId)
            return;

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            MERGE dbo.StudyNoteSubjectRelations AS target
            USING (SELECT @NoteId AS NoteId, @SubjectId AS SubjectId) AS source
                ON target.NoteId = source.NoteId
               AND target.SubjectId = source.SubjectId
               AND target.RelationSource = 0
            WHEN NOT MATCHED THEN
                INSERT (NoteId, SubjectId, RelationSource, Score, ClassificationRunId)
                VALUES (source.NoteId, source.SubjectId, 0, NULL, NULL);
            """;
        command.AddParameter("@NoteId", DbType.Guid, studyNote.Id);
        command.AddParameter("@SubjectId", DbType.Guid, subjectId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task EnqueueClassificationAsync(
        DbConnection connection,
        DbTransaction transaction,
        StudyNote studyNote,
        CancellationToken ct
    )
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO dbo.ClassificationJobs
                (Id, NoteId, NoteVersion, TaxonomyVersion, Status, Attempts, AvailableAtUtc)
            SELECT NEWID(), @NoteId, @NoteVersion, TaxonomyVersion, 0, 0, SYSUTCDATETIME()
            FROM dbo.ClassificationTaxonomyState
            WHERE Id = 1;
            """;
        command.AddParameter("@NoteId", DbType.Guid, studyNote.Id);
        command.AddParameter("@NoteVersion", DbType.Int64, studyNote.Version);
        await command.ExecuteNonQueryAsync(ct);
    }

    private sealed class StudyNoteRow(
        Guid id,
        Guid? subjectId,
        Guid? topicId,
        string title,
        string content,
        TimeSpan studyDuration,
        DateTimeOffset studyStartedAtUtc,
        long version,
        NoteClassificationStatus classificationStatus,
        string? failureReason,
        string? model,
        string? modelVersion
    )
    {
        public Guid Id { get; } = id;
        public Guid? SubjectId { get; } = subjectId;
        public Guid? TopicId { get; } = topicId;
        public string Title { get; } = title;
        public string Content { get; } = content;
        public TimeSpan StudyDuration { get; } = studyDuration;
        public DateTimeOffset StudyStartedAtUtc { get; } = studyStartedAtUtc;
        public long Version { get; } = version;
        public NoteClassificationStatus ClassificationStatus { get; } = classificationStatus;
        public string? FailureReason { get; } = failureReason;
        public string? Model { get; } = model;
        public string? ModelVersion { get; } = modelVersion;
        public Dictionary<Guid, StudyNoteMetric> Metrics { get; } = [];
        public Dictionary<Guid, NoteClassificationScore> Classifications { get; } = [];
    }
}
