using System.Data;
using System.Data.Common;
using System.Text;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Database;

namespace KnowledgeTracker.Data.Knowledge.Repositories;

public sealed class SqlServerClassificationJobRepository(Func<DbConnection> connectionFactory)
    : IClassificationJobRepository
{
    public async Task<ClassificationJob?> ClaimNextAsync(
        string workerId,
        TimeSpan leaseDuration,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(workerId))
            throw new ArgumentException("Worker identifier is required.", nameof(workerId));
        if (leaseDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));

        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SET NOCOUNT ON;

            UPDATE dbo.ClassificationJobs
            SET Status = 4,
                LockedUntilUtc = NULL,
                WorkerId = NULL,
                CompletedAtUtc = SYSUTCDATETIME(),
                LastError = COALESCE(LastError, 'Classification lease expired after the maximum number of attempts.')
            WHERE Status IN (1, 2)
              AND Attempts >= 3
              AND (Status = 2 OR LockedUntilUtc < SYSUTCDATETIME());

            ;WITH NextJob AS
            (
                SELECT TOP (1) job.*
                FROM dbo.ClassificationJobs AS job WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE
                    (job.Status IN (0, 2) AND job.Attempts < 3 AND job.AvailableAtUtc <= SYSUTCDATETIME())
                    OR (job.Status = 1 AND job.Attempts < 3 AND job.LockedUntilUtc < SYSUTCDATETIME())
                ORDER BY job.AvailableAtUtc, job.CreatedAtUtc, job.Id
            )
            UPDATE NextJob
            SET Status = 1,
                Attempts = Attempts + 1,
                WorkerId = @WorkerId,
                LockedUntilUtc = DATEADD(SECOND, @LeaseSeconds, SYSUTCDATETIME()),
                StartedAtUtc = COALESCE(StartedAtUtc, SYSUTCDATETIME()),
                LastError = NULL
            OUTPUT inserted.Id, inserted.NoteId, inserted.NoteVersion, inserted.TaxonomyVersion,
                   inserted.Attempts, inserted.WorkerId;
            """;
        command.AddParameter("@WorkerId", DbType.String, workerId.Trim());
        command.AddParameter("@LeaseSeconds", DbType.Int32, checked((int)Math.Ceiling(leaseDuration.TotalSeconds)));
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return new ClassificationJob(
            reader.GetGuid(0), reader.GetGuid(1), reader.GetInt64(2), reader.GetInt64(3),
            reader.GetInt32(4), reader.GetString(5)
        );
    }

    public async Task<ClassificationWorkItem?> LoadWorkItemAsync(
        ClassificationJob job,
        CancellationToken ct
    )
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        await using var versionCommand = connection.CreateCommand();
        versionCommand.Transaction = transaction;
        versionCommand.CommandText = """
            SELECT note.NoteVersion, note.Title, note.Content, taxonomy.TaxonomyVersion
            FROM dbo.StudyNotes AS note
            CROSS JOIN dbo.ClassificationTaxonomyState AS taxonomy WITH (UPDLOCK)
            WHERE note.Id = @NoteId AND taxonomy.Id = 1;
            """;
        versionCommand.AddParameter("@NoteId", DbType.Guid, job.NoteId);
        await using var versionReader = await versionCommand.ExecuteReaderAsync(ct);
        if (!await versionReader.ReadAsync(ct))
        {
            await transaction.RollbackAsync(ct);
            return null;
        }

        var currentNoteVersion = versionReader.GetInt64(0);
        var text = string.Concat(versionReader.GetString(1), Environment.NewLine, Environment.NewLine, versionReader.GetString(2));
        var currentTaxonomyVersion = versionReader.GetInt64(3);
        await versionReader.DisposeAsync();

        if (currentNoteVersion != job.NoteVersion || currentTaxonomyVersion != job.TaxonomyVersion)
        {
            await SupersedeAndEnqueueCurrentAsync(
                connection, transaction, job, currentNoteVersion, currentTaxonomyVersion, ct
            );
            await transaction.CommitAsync(ct);
            return null;
        }

        await using var nodesCommand = connection.CreateCommand();
        nodesCommand.Transaction = transaction;
        nodesCommand.CommandText = """
            SELECT Id, Name, Description, ParentSubjectId
            FROM dbo.Subjects
            ORDER BY ParentSubjectId, Name, Id;
            """;
        var nodes = new List<ClassificationNode>();
        await using var nodesReader = await nodesCommand.ExecuteReaderAsync(ct);
        while (await nodesReader.ReadAsync(ct))
            nodes.Add(new ClassificationNode(
                nodesReader.GetGuid(0),
                nodesReader.GetString(1),
                nodesReader.IsDBNull(2) ? null : nodesReader.GetString(2),
                nodesReader.IsDBNull(3) ? null : nodesReader.GetGuid(3)
            ));

        await transaction.CommitAsync(ct);
        return new ClassificationWorkItem(job, text, nodes);
    }

    public async Task<ClassificationCompletionOutcome> CompleteAsync(
        ClassificationJob job,
        ClassifierResult result,
        double relationThreshold,
        CancellationToken ct
    )
    {
        if (!double.IsFinite(relationThreshold) || relationThreshold < 0 || relationThreshold > 1)
            throw new ArgumentOutOfRangeException(nameof(relationThreshold));

        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var current = await ReadCurrentVersionsAsync(connection, transaction, job.NoteId, ct);
        if (current is null)
        {
            await transaction.RollbackAsync(ct);
            return ClassificationCompletionOutcome.Superseded;
        }

        if (current.Value.NoteVersion != job.NoteVersion || current.Value.TaxonomyVersion != job.TaxonomyVersion)
        {
            await SupersedeAndEnqueueCurrentAsync(
                connection, transaction, job, current.Value.NoteVersion, current.Value.TaxonomyVersion, ct
            );
            await transaction.CommitAsync(ct);
            return ClassificationCompletionOutcome.Superseded;
        }

        var runId = Guid.NewGuid();
        await using (var runCommand = connection.CreateCommand())
        {
            runCommand.Transaction = transaction;
            runCommand.CommandText = """
                IF NOT EXISTS (SELECT 1 FROM dbo.ClassificationRuns WHERE ClassificationJobId = @JobId)
                BEGIN
                    INSERT INTO dbo.ClassificationRuns
                        (Id, ClassificationJobId, NoteId, NoteVersion, TaxonomyVersion, Model, ModelVersion)
                    VALUES
                        (@Id, @JobId, @NoteId, @NoteVersion, @TaxonomyVersion, @Model, @ModelVersion);
                END;
                """;
            runCommand.AddParameter("@Id", DbType.Guid, runId);
            runCommand.AddParameter("@JobId", DbType.Guid, job.Id);
            runCommand.AddParameter("@NoteId", DbType.Guid, job.NoteId);
            runCommand.AddParameter("@NoteVersion", DbType.Int64, job.NoteVersion);
            runCommand.AddParameter("@TaxonomyVersion", DbType.Int64, job.TaxonomyVersion);
            runCommand.AddParameter("@Model", DbType.String, result.Model.Trim());
            runCommand.AddParameter("@ModelVersion", DbType.String, result.ModelVersion.Trim());
            await runCommand.ExecuteNonQueryAsync(ct);
        }

        await using (var resolveRunCommand = connection.CreateCommand())
        {
            resolveRunCommand.Transaction = transaction;
            resolveRunCommand.CommandText = "SELECT Id FROM dbo.ClassificationRuns WHERE ClassificationJobId = @JobId;";
            resolveRunCommand.AddParameter("@JobId", DbType.Guid, job.Id);
            runId = (Guid)(await resolveRunCommand.ExecuteScalarAsync(ct)
                ?? throw new InvalidOperationException("Classification run could not be resolved."));
        }

        await ReplaceScoresAsync(connection, transaction, runId, result.Classifications, ct);
        await ReplaceClassifierRelationsAsync(
            connection, transaction, job.NoteId, runId, result.Classifications, relationThreshold, ct
        );

        await using var completeCommand = connection.CreateCommand();
        completeCommand.Transaction = transaction;
        completeCommand.CommandText = """
            UPDATE dbo.ClassificationJobs
            SET Status = 3,
                LockedUntilUtc = NULL,
                WorkerId = NULL,
                CompletedAtUtc = SYSUTCDATETIME(),
                LastError = NULL
            WHERE Id = @Id AND Status = 1 AND WorkerId = @WorkerId;
            """;
        completeCommand.AddParameter("@Id", DbType.Guid, job.Id);
        completeCommand.AddParameter("@WorkerId", DbType.String, job.WorkerId);
        if (await completeCommand.ExecuteNonQueryAsync(ct) != 1)
            throw new DBConcurrencyException("The classification lease is no longer owned by this worker.");

        await transaction.CommitAsync(ct);
        return ClassificationCompletionOutcome.Completed;
    }

    public async Task RecordFailureAsync(
        ClassificationJob job,
        string error,
        DateTimeOffset? retryAtUtc,
        CancellationToken ct
    )
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.ClassificationJobs
            SET Status = CASE WHEN @RetryAtUtc IS NULL THEN 4 ELSE 2 END,
                AvailableAtUtc = COALESCE(@RetryAtUtc, AvailableAtUtc),
                LockedUntilUtc = NULL,
                WorkerId = NULL,
                CompletedAtUtc = CASE WHEN @RetryAtUtc IS NULL THEN SYSUTCDATETIME() ELSE NULL END,
                LastError = @LastError
            WHERE Id = @Id AND Status = 1 AND WorkerId = @WorkerId;
            """;
        command.AddParameter("@RetryAtUtc", DbType.DateTimeOffset, retryAtUtc is null ? DBNull.Value : retryAtUtc.Value);
        command.AddParameter("@LastError", DbType.String, Truncate(error, 2000));
        command.AddParameter("@Id", DbType.Guid, job.Id);
        command.AddParameter("@WorkerId", DbType.String, job.WorkerId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<(long NoteVersion, long TaxonomyVersion)?> ReadCurrentVersionsAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid noteId,
        CancellationToken ct
    )
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT note.NoteVersion, taxonomy.TaxonomyVersion
            FROM dbo.StudyNotes AS note WITH (UPDLOCK)
            CROSS JOIN dbo.ClassificationTaxonomyState AS taxonomy WITH (UPDLOCK)
            WHERE note.Id = @NoteId AND taxonomy.Id = 1;
            """;
        command.AddParameter("@NoteId", DbType.Guid, noteId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? (reader.GetInt64(0), reader.GetInt64(1)) : null;
    }

    private static async Task SupersedeAndEnqueueCurrentAsync(
        DbConnection connection,
        DbTransaction transaction,
        ClassificationJob job,
        long currentNoteVersion,
        long currentTaxonomyVersion,
        CancellationToken ct
    )
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE dbo.ClassificationJobs
            SET Status = 5,
                LockedUntilUtc = NULL,
                WorkerId = NULL,
                CompletedAtUtc = SYSUTCDATETIME(),
                LastError = 'Superseded by a newer note or taxonomy version.'
            WHERE Id = @JobId AND Status = 1 AND WorkerId = @WorkerId;

            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.ClassificationJobs
                WHERE NoteId = @NoteId
                  AND NoteVersion = @NoteVersion
                  AND TaxonomyVersion = @TaxonomyVersion
            )
            BEGIN
                INSERT INTO dbo.ClassificationJobs
                    (Id, NoteId, NoteVersion, TaxonomyVersion, Status, Attempts, AvailableAtUtc)
                VALUES
                    (NEWID(), @NoteId, @NoteVersion, @TaxonomyVersion, 0, 0, SYSUTCDATETIME());
            END;
            """;
        command.AddParameter("@JobId", DbType.Guid, job.Id);
        command.AddParameter("@WorkerId", DbType.String, job.WorkerId);
        command.AddParameter("@NoteId", DbType.Guid, job.NoteId);
        command.AddParameter("@NoteVersion", DbType.Int64, currentNoteVersion);
        command.AddParameter("@TaxonomyVersion", DbType.Int64, currentTaxonomyVersion);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task ReplaceScoresAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid runId,
        IReadOnlyCollection<ClassifierScore> scores,
        CancellationToken ct
    )
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM dbo.NoteClassifications WHERE ClassificationRunId = @RunId;";
        command.AddParameter("@RunId", DbType.Guid, runId);
        await command.ExecuteNonQueryAsync(ct);
        if (scores.Count == 0)
            return;

        command.Parameters.Clear();
        var sql = new StringBuilder("INSERT INTO dbo.NoteClassifications (ClassificationRunId, SubjectId, SubjectName, Score) ");
        for (var index = 0; index < scores.Count; index++)
        {
            if (index > 0)
                sql.Append(" UNION ALL ");
            sql.Append($"SELECT @RunId, subject.Id, subject.Name, @Score{index} FROM dbo.Subjects AS subject WHERE subject.Id = @SubjectId{index}");
        }
        command.CommandText = sql.ToString();
        command.AddParameter("@RunId", DbType.Guid, runId);
        var scoreArray = scores.ToArray();
        for (var index = 0; index < scoreArray.Length; index++)
        {
            command.AddParameter($"@SubjectId{index}", DbType.Guid, scoreArray[index].NodeId);
            command.AddParameter($"@Score{index}", DbType.Decimal, Convert.ToDecimal(scoreArray[index].Score));
        }
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task ReplaceClassifierRelationsAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid noteId,
        Guid runId,
        IReadOnlyCollection<ClassifierScore> scores,
        double threshold,
        CancellationToken ct
    )
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM dbo.StudyNoteSubjectRelations WHERE NoteId = @NoteId AND RelationSource = 1;";
        command.AddParameter("@NoteId", DbType.Guid, noteId);
        await command.ExecuteNonQueryAsync(ct);

        var selected = scores.Where(item => item.Score >= threshold).ToArray();
        if (selected.Length == 0)
            return;

        command.Parameters.Clear();
        var sql = new StringBuilder("INSERT INTO dbo.StudyNoteSubjectRelations (NoteId, SubjectId, RelationSource, Score, ClassificationRunId) VALUES ");
        for (var index = 0; index < selected.Length; index++)
        {
            if (index > 0)
                sql.Append(',');
            sql.Append($"(@NoteId, @SubjectId{index}, 1, @Score{index}, @RunId)");
        }
        command.CommandText = sql.ToString();
        command.AddParameter("@NoteId", DbType.Guid, noteId);
        command.AddParameter("@RunId", DbType.Guid, runId);
        for (var index = 0; index < selected.Length; index++)
        {
            command.AddParameter($"@SubjectId{index}", DbType.Guid, selected[index].NodeId);
            command.AddParameter($"@Score{index}", DbType.Decimal, Convert.ToDecimal(selected[index].Score));
        }
        await command.ExecuteNonQueryAsync(ct);
    }

    private static string Truncate(string? value, int maximumLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "Classification failed." : value.Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }
}
