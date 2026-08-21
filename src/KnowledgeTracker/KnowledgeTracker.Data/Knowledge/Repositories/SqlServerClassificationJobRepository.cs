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
            SELECT note.NoteVersion,
                   note.Title,
                   note.Content,
                   taxonomy.TaxonomyVersion,
                   CAST(CASE WHEN note.SubjectId IS NULL AND note.TopicId IS NULL THEN 0 ELSE 1 END AS BIT)
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
        var hasOwnership = versionReader.GetBoolean(4);
        await versionReader.DisposeAsync();

        if (hasOwnership || currentNoteVersion != job.NoteVersion || currentTaxonomyVersion != job.TaxonomyVersion)
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
            WITH SubjectPaths AS
            (
                SELECT subject.Id, subject.Name, subject.Description,
                       CAST(subject.Name AS NVARCHAR(MAX)) AS SubjectPath
                FROM dbo.Subjects AS subject
                WHERE subject.ParentSubjectId IS NULL

                UNION ALL

                SELECT child.Id, child.Name, child.Description,
                       CAST(parent.SubjectPath + N' > ' + child.Name AS NVARCHAR(MAX))
                FROM dbo.Subjects AS child
                INNER JOIN SubjectPaths AS parent ON parent.Id = child.ParentSubjectId
            )
            , LeafSubjects AS
            (
                SELECT path.Id, path.SubjectPath, path.Description
                FROM SubjectPaths AS path
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM dbo.Subjects AS child
                    WHERE child.ParentSubjectId = path.Id
                )
            )
            SELECT node.Id, node.Name, node.Description, node.ParentId
            FROM
            (
                SELECT subject.Id,
                       subject.SubjectPath AS Name,
                       subject.Description,
                       CAST(NULL AS UNIQUEIDENTIFIER) AS ParentId,
                       subject.SubjectPath AS SortPath,
                       0 AS SortOrder
                FROM LeafSubjects AS subject

                UNION ALL

                SELECT topic.Id,
                       CAST(topic.Name AS NVARCHAR(MAX)),
                       CAST(NULL AS NVARCHAR(MAX)),
                       topic.SubjectId,
                       CAST(subject.SubjectPath + N' > ' + topic.Name AS NVARCHAR(MAX)),
                       1
                FROM dbo.Topics AS topic
                INNER JOIN LeafSubjects AS subject ON subject.Id = topic.SubjectId
            ) AS node
            ORDER BY node.SortPath, node.SortOrder, node.Id
            OPTION (MAXRECURSION 100);
            """;
        var nodes = new List<ClassificationNode>();
        await using (var nodesReader = await nodesCommand.ExecuteReaderAsync(ct))
        {
            while (await nodesReader.ReadAsync(ct))
                nodes.Add(new ClassificationNode(
                    nodesReader.GetGuid(0),
                    nodesReader.GetString(1),
                    nodesReader.IsDBNull(2) ? null : nodesReader.GetString(2),
                    nodesReader.IsDBNull(3) ? null : nodesReader.GetGuid(3)
                ));
        }

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

        if (current.Value.HasOwnership
            || current.Value.NoteVersion != job.NoteVersion
            || current.Value.TaxonomyVersion != job.TaxonomyVersion)
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
        await AssignUnclassifiedOwnershipAsync(connection, transaction, job.NoteId, result.Classifications, ct);
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

    private static async Task<(long NoteVersion, long TaxonomyVersion, bool HasOwnership)?> ReadCurrentVersionsAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid noteId,
        CancellationToken ct
    )
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT note.NoteVersion,
                   taxonomy.TaxonomyVersion,
                   CAST(CASE WHEN note.SubjectId IS NULL AND note.TopicId IS NULL THEN 0 ELSE 1 END AS BIT)
            FROM dbo.StudyNotes AS note WITH (UPDLOCK)
            CROSS JOIN dbo.ClassificationTaxonomyState AS taxonomy WITH (UPDLOCK)
            WHERE note.Id = @NoteId AND taxonomy.Id = 1;
            """;
        command.AddParameter("@NoteId", DbType.Guid, noteId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct)
            ? (reader.GetInt64(0), reader.GetInt64(1), reader.GetBoolean(2))
            : null;
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
                LastError = 'Superseded because the note is already classified or its version changed.'
            WHERE Id = @JobId AND Status = 1 AND WorkerId = @WorkerId;

            IF NOT EXISTS
            (
                SELECT 1
                FROM dbo.ClassificationJobs
                WHERE NoteId = @NoteId
                  AND NoteVersion = @NoteVersion
                  AND TaxonomyVersion = @TaxonomyVersion
            )
            AND EXISTS
            (
                SELECT 1
                FROM dbo.StudyNotes
                WHERE Id = @NoteId
                  AND SubjectId IS NULL
                  AND TopicId IS NULL
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
        var sql = new StringBuilder("WITH NodeScores (NodeId, Score) AS (");
        for (var index = 0; index < scores.Count; index++)
        {
            if (index > 0)
                sql.Append(" UNION ALL ");
            sql.Append($"SELECT @NodeId{index}, @Score{index}");
        }
        sql.Append(") INSERT INTO dbo.NoteClassifications (ClassificationRunId, SubjectId, SubjectName, Score) ");
        sql.Append("SELECT @RunId, subject.Id, subject.Name, scores.Score FROM NodeScores AS scores ");
        sql.Append("INNER JOIN dbo.Subjects AS subject ON subject.Id = scores.NodeId ");
        sql.Append("WHERE NOT EXISTS (SELECT 1 FROM dbo.Subjects AS child WHERE child.ParentSubjectId = subject.Id);");
        command.CommandText = sql.ToString();
        command.AddParameter("@RunId", DbType.Guid, runId);
        var scoreArray = scores.ToArray();
        for (var index = 0; index < scoreArray.Length; index++)
        {
            command.AddParameter($"@NodeId{index}", DbType.Guid, scoreArray[index].NodeId);
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

        if (scores.Count == 0)
            return;

        command.Parameters.Clear();
        var scoreArray = scores.ToArray();
        var sql = new StringBuilder("WITH NodeScores (NodeId, Score) AS (");
        for (var index = 0; index < scoreArray.Length; index++)
        {
            if (index > 0)
                sql.Append(" UNION ALL ");
            sql.Append($"SELECT @NodeId{index}, @Score{index}");
        }
        sql.Append(") INSERT INTO dbo.StudyNoteSubjectRelations (NoteId, SubjectId, RelationSource, Score, ClassificationRunId) ");
        sql.Append("SELECT @NoteId, subject.Id, 1, scores.Score, @RunId FROM NodeScores AS scores ");
        sql.Append("INNER JOIN dbo.Subjects AS subject ON subject.Id = scores.NodeId ");
        sql.Append("WHERE NOT EXISTS (SELECT 1 FROM dbo.Subjects AS child WHERE child.ParentSubjectId = subject.Id) ");
        sql.Append("AND scores.Score >= @Threshold;");
        command.CommandText = sql.ToString();
        command.AddParameter("@NoteId", DbType.Guid, noteId);
        command.AddParameter("@RunId", DbType.Guid, runId);
        command.AddParameter("@Threshold", DbType.Decimal, Convert.ToDecimal(threshold));
        for (var index = 0; index < scoreArray.Length; index++)
        {
            command.AddParameter($"@NodeId{index}", DbType.Guid, scoreArray[index].NodeId);
            command.AddParameter($"@Score{index}", DbType.Decimal, Convert.ToDecimal(scoreArray[index].Score));
        }
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task AssignUnclassifiedOwnershipAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid noteId,
        IReadOnlyCollection<ClassifierScore> scores,
        CancellationToken ct
    )
    {
        if (scores.Count == 0)
            return;

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        var scoreArray = scores.ToArray();
        var sql = new StringBuilder("WITH NodeScores (NodeId, Score) AS (");
        for (var index = 0; index < scoreArray.Length; index++)
        {
            if (index > 0)
                sql.Append(" UNION ALL ");
            sql.Append($"SELECT @NodeId{index}, @Score{index}");
        }
        sql.Append("), BestSubject AS (");
        sql.Append("SELECT TOP (1) subject.Id AS SubjectId, scores.Score FROM NodeScores AS scores ");
        sql.Append("INNER JOIN dbo.Subjects AS subject ON subject.Id = scores.NodeId ");
        sql.Append("WHERE NOT EXISTS (SELECT 1 FROM dbo.Subjects AS child WHERE child.ParentSubjectId = subject.Id) ");
        sql.Append("AND EXISTS (SELECT 1 FROM dbo.Topics AS topic WHERE topic.SubjectId = subject.Id) ");
        sql.Append("ORDER BY scores.Score DESC, subject.Id), BestTopic AS (");
        sql.Append("SELECT TOP (1) topic.Id AS TopicId, best.SubjectId FROM BestSubject AS best ");
        sql.Append("INNER JOIN dbo.Topics AS topic ON topic.SubjectId = best.SubjectId ");
        sql.Append("LEFT JOIN NodeScores AS scores ON scores.NodeId = topic.Id ");
        sql.Append("ORDER BY COALESCE(scores.Score, 0) DESC, topic.Id) ");
        sql.Append("UPDATE note SET SubjectId = best.SubjectId, TopicId = best.TopicId ");
        sql.Append("FROM dbo.StudyNotes AS note CROSS JOIN BestTopic AS best ");
        sql.Append("WHERE note.Id = @NoteId AND note.SubjectId IS NULL AND note.TopicId IS NULL;");
        command.CommandText = sql.ToString();
        command.AddParameter("@NoteId", DbType.Guid, noteId);
        for (var index = 0; index < scoreArray.Length; index++)
        {
            command.AddParameter($"@NodeId{index}", DbType.Guid, scoreArray[index].NodeId);
            command.AddParameter($"@Score{index}", DbType.Decimal, Convert.ToDecimal(scoreArray[index].Score));
        }
        await command.ExecuteNonQueryAsync(ct);
    }

    private static string Truncate(string? value, int maximumLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "Classification failed." : value.Trim();
        return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
    }
}
