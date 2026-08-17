using System.Data;
using System.Data.Common;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Data.Knowledge.Repositories;

public sealed class SqlServerSubjectGoalRepository(Func<DbConnection> connectionFactory) : ISubjectGoalRepository
{
    public async Task<IReadOnlyCollection<SubjectGoal>> ListBySubjectAsync(Guid subjectId, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, SubjectId, TopicId, Title, GoalKind, MetricDefinitionId, TargetValue, TargetDate, GoalPeriod, CustomPeriodStartDate, CustomPeriodEndDate, PriorityPosition, IsCompleted, CompletedAtUtc, CreatedAtUtc FROM dbo.SubjectGoals WHERE SubjectId = @SubjectId ORDER BY PriorityPosition;";
        command.AddParameter("@SubjectId", DbType.Guid, subjectId); await using var reader = await command.ExecuteReaderAsync(ct);
        var goals = new List<SubjectGoal>(); while (await reader.ReadAsync(ct)) goals.Add(ReadGoal(reader)); return goals;
    }

    public async Task<SubjectGoal?> FindAsync(Guid id, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, SubjectId, TopicId, Title, GoalKind, MetricDefinitionId, TargetValue, TargetDate, GoalPeriod, CustomPeriodStartDate, CustomPeriodEndDate, PriorityPosition, IsCompleted, CompletedAtUtc, CreatedAtUtc FROM dbo.SubjectGoals WHERE Id = @Id;";
        command.AddParameter("@Id", DbType.Guid, id); await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadGoal(reader) : null;
    }

    public async Task AddAsync(SubjectGoal goal, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO dbo.SubjectGoals (Id, SubjectId, TopicId, Title, GoalKind, MetricDefinitionId, TargetValue, TargetDate, GoalPeriod, CustomPeriodStartDate, CustomPeriodEndDate, PriorityPosition, IsCompleted, CompletedAtUtc, CreatedAtUtc) VALUES (@Id, @SubjectId, @TopicId, @Title, @GoalKind, @MetricDefinitionId, @TargetValue, @TargetDate, @GoalPeriod, @CustomPeriodStartDate, @CustomPeriodEndDate, (SELECT ISNULL(MAX(PriorityPosition), 0) + 1 FROM dbo.SubjectGoals WITH (TABLOCKX)), @IsCompleted, @CompletedAtUtc, @CreatedAtUtc);";
        command.AddParameter("@Id", DbType.Guid, goal.Id); command.AddParameter("@SubjectId", DbType.Guid, goal.SubjectId); command.AddParameter("@TopicId", DbType.Guid, goal.TopicId); command.AddParameter("@Title", DbType.String, goal.Title); command.AddParameter("@GoalKind", DbType.Byte, (byte)goal.Kind); command.AddParameter("@MetricDefinitionId", DbType.Guid, (object?)goal.MetricDefinitionId ?? DBNull.Value); command.AddParameter("@TargetValue", DbType.Decimal, (object?)goal.TargetValue ?? DBNull.Value); command.AddParameter("@TargetDate", DbType.Date, (object?)goal.TargetDate ?? DBNull.Value); command.AddParameter("@GoalPeriod", DbType.Byte, (byte)goal.Period); command.AddParameter("@CustomPeriodStartDate", DbType.Date, (object?)goal.CustomPeriodStartDate ?? DBNull.Value); command.AddParameter("@CustomPeriodEndDate", DbType.Date, (object?)goal.CustomPeriodEndDate ?? DBNull.Value); command.AddParameter("@IsCompleted", DbType.Boolean, goal.IsCompleted); command.AddParameter("@CompletedAtUtc", DbType.DateTimeOffset, (object?)goal.CompletedAtUtc ?? DBNull.Value); command.AddParameter("@CreatedAtUtc", DbType.DateTimeOffset, goal.CreatedAtUtc); await command.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateAsync(SubjectGoal goal, IReadOnlyCollection<SubjectSubGoal> subGoals, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "UPDATE dbo.SubjectGoals SET TopicId = @TopicId, Title = @Title, GoalKind = @GoalKind, MetricDefinitionId = @MetricDefinitionId, TargetValue = @TargetValue, TargetDate = @TargetDate, GoalPeriod = @GoalPeriod, CustomPeriodStartDate = @CustomPeriodStartDate, CustomPeriodEndDate = @CustomPeriodEndDate, IsCompleted = @IsCompleted, CompletedAtUtc = @CompletedAtUtc WHERE Id = @Id; DELETE FROM dbo.SubjectSubGoals WHERE SubjectGoalId = @Id;";
            command.AddParameter("@Id", DbType.Guid, goal.Id); command.AddParameter("@TopicId", DbType.Guid, goal.TopicId); command.AddParameter("@Title", DbType.String, goal.Title); command.AddParameter("@GoalKind", DbType.Byte, (byte)goal.Kind); command.AddParameter("@MetricDefinitionId", DbType.Guid, (object?)goal.MetricDefinitionId ?? DBNull.Value); command.AddParameter("@TargetValue", DbType.Decimal, (object?)goal.TargetValue ?? DBNull.Value); command.AddParameter("@TargetDate", DbType.Date, (object?)goal.TargetDate ?? DBNull.Value); command.AddParameter("@GoalPeriod", DbType.Byte, (byte)goal.Period); command.AddParameter("@CustomPeriodStartDate", DbType.Date, (object?)goal.CustomPeriodStartDate ?? DBNull.Value); command.AddParameter("@CustomPeriodEndDate", DbType.Date, (object?)goal.CustomPeriodEndDate ?? DBNull.Value); command.AddParameter("@IsCompleted", DbType.Boolean, goal.IsCompleted); command.AddParameter("@CompletedAtUtc", DbType.DateTimeOffset, (object?)goal.CompletedAtUtc ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(ct);
        }
        foreach (var subGoal in subGoals)
        {
            await using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "INSERT INTO dbo.SubjectSubGoals (Id, SubjectGoalId, Title, IsCompleted, CompletedAtUtc, CreatedAtUtc) VALUES (@Id, @SubjectGoalId, @Title, @IsCompleted, @CompletedAtUtc, @CreatedAtUtc);";
            command.AddParameter("@Id", DbType.Guid, subGoal.Id); command.AddParameter("@SubjectGoalId", DbType.Guid, subGoal.SubjectGoalId); command.AddParameter("@Title", DbType.String, subGoal.Title); command.AddParameter("@IsCompleted", DbType.Boolean, subGoal.IsCompleted); command.AddParameter("@CompletedAtUtc", DbType.DateTimeOffset, (object?)subGoal.CompletedAtUtc ?? DBNull.Value); command.AddParameter("@CreatedAtUtc", DbType.DateTimeOffset, subGoal.CreatedAtUtc);
            await command.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand(); command.CommandText = "DELETE FROM dbo.SubjectGoals WHERE Id = @Id;"; command.AddParameter("@Id", DbType.Guid, id); return await command.ExecuteNonQueryAsync(ct) > 0;
    }
    public async Task<bool> CompleteAsync(Guid id, DateTimeOffset completedAtUtc, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand(); command.CommandText = "UPDATE dbo.SubjectGoals SET IsCompleted = 1, CompletedAtUtc = @CompletedAtUtc WHERE Id = @Id AND GoalKind = 2 AND IsCompleted = 0;"; command.AddParameter("@Id", DbType.Guid, id); command.AddParameter("@CompletedAtUtc", DbType.DateTimeOffset, completedAtUtc); return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task AddSubGoalsAsync(IReadOnlyCollection<SubjectSubGoal> subGoals, CancellationToken ct)
    {
        if (subGoals.Count == 0) return;
        await using var connection = connectionFactory(); await connection.OpenAsync(ct);
        foreach (var subGoal in subGoals)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO dbo.SubjectSubGoals (Id, SubjectGoalId, Title, IsCompleted, CompletedAtUtc, CreatedAtUtc) VALUES (@Id, @SubjectGoalId, @Title, @IsCompleted, @CompletedAtUtc, @CreatedAtUtc);";
            command.AddParameter("@Id", DbType.Guid, subGoal.Id); command.AddParameter("@SubjectGoalId", DbType.Guid, subGoal.SubjectGoalId); command.AddParameter("@Title", DbType.String, subGoal.Title); command.AddParameter("@IsCompleted", DbType.Boolean, subGoal.IsCompleted); command.AddParameter("@CompletedAtUtc", DbType.DateTimeOffset, (object?)subGoal.CompletedAtUtc ?? DBNull.Value); command.AddParameter("@CreatedAtUtc", DbType.DateTimeOffset, subGoal.CreatedAtUtc); await command.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task<IReadOnlyCollection<SubjectSubGoal>> ListSubGoalsAsync(IReadOnlyCollection<Guid> subjectGoalIds, CancellationToken ct)
    {
        if (subjectGoalIds.Count == 0) return [];
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        var parameters = subjectGoalIds.Select((id, index) => { var name = $"@Id{index}"; command.AddParameter(name, DbType.Guid, id); return name; });
        command.CommandText = $"SELECT Id, SubjectGoalId, Title, IsCompleted, CompletedAtUtc, CreatedAtUtc FROM dbo.SubjectSubGoals WHERE SubjectGoalId IN ({string.Join(',', parameters)}) ORDER BY CreatedAtUtc;";
        await using var reader = await command.ExecuteReaderAsync(ct); var subGoals = new List<SubjectSubGoal>();
        while (await reader.ReadAsync(ct)) subGoals.Add(new SubjectSubGoal(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetBoolean(3), reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4), reader.GetFieldValue<DateTimeOffset>(5)));
        return subGoals;
    }

    public async Task<IReadOnlyCollection<SubjectGoalDayRecord>> ListDayRecordsAsync(IReadOnlyCollection<Guid> subjectGoalIds, CancellationToken ct)
    {
        if (subjectGoalIds.Count == 0) return [];
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        var parameters = subjectGoalIds.Select((id, index) => { var name = $"@Id{index}"; command.AddParameter(name, DbType.Guid, id); return name; });
        command.CommandText = $"SELECT Id, SubjectGoalId, OccurredOn, IsCompleted, RecordedAtUtc FROM dbo.SubjectGoalDayRecords WHERE SubjectGoalId IN ({string.Join(',', parameters)}) ORDER BY OccurredOn DESC;";
        await using var reader = await command.ExecuteReaderAsync(ct); var records = new List<SubjectGoalDayRecord>();
        while (await reader.ReadAsync(ct)) records.Add(new SubjectGoalDayRecord(reader.GetGuid(0), reader.GetGuid(1), DateOnly.FromDateTime(reader.GetDateTime(2)), reader.GetBoolean(3), reader.GetFieldValue<DateTimeOffset>(4)));
        return records;
    }

    public async Task<SubjectGoalDayRecord> UpsertDayRecordAsync(SubjectGoalDayRecord dayRecord, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "MERGE dbo.SubjectGoalDayRecords WITH (HOLDLOCK) AS Target USING (VALUES (@SubjectGoalId, @OccurredOn, @Id, @IsCompleted, @RecordedAtUtc)) AS Source (SubjectGoalId, OccurredOn, Id, IsCompleted, RecordedAtUtc) ON Target.SubjectGoalId = Source.SubjectGoalId AND Target.OccurredOn = Source.OccurredOn WHEN MATCHED THEN UPDATE SET IsCompleted = Source.IsCompleted, RecordedAtUtc = Source.RecordedAtUtc WHEN NOT MATCHED THEN INSERT (Id, SubjectGoalId, OccurredOn, IsCompleted, RecordedAtUtc) VALUES (Source.Id, Source.SubjectGoalId, Source.OccurredOn, Source.IsCompleted, Source.RecordedAtUtc) OUTPUT inserted.Id, inserted.SubjectGoalId, inserted.OccurredOn, inserted.IsCompleted, inserted.RecordedAtUtc;";
        command.AddParameter("@Id", DbType.Guid, dayRecord.Id); command.AddParameter("@SubjectGoalId", DbType.Guid, dayRecord.SubjectGoalId); command.AddParameter("@OccurredOn", DbType.Date, dayRecord.OccurredOn); command.AddParameter("@IsCompleted", DbType.Boolean, dayRecord.IsCompleted); command.AddParameter("@RecordedAtUtc", DbType.DateTimeOffset, dayRecord.RecordedAtUtc);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new InvalidOperationException("The goal day record could not be saved.");
        return new SubjectGoalDayRecord(reader.GetGuid(0), reader.GetGuid(1), DateOnly.FromDateTime(reader.GetDateTime(2)), reader.GetBoolean(3), reader.GetFieldValue<DateTimeOffset>(4));
    }

    public async Task<bool> SetSubGoalCompletionAsync(Guid id, bool isCompleted, DateTimeOffset changedAtUtc, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.SubjectSubGoals SET IsCompleted = @IsCompleted, CompletedAtUtc = @CompletedAtUtc WHERE Id = @Id AND EXISTS (SELECT 1 FROM dbo.SubjectGoals WHERE Id = SubjectGoalId AND GoalKind = 2);";
        command.AddParameter("@Id", DbType.Guid, id); command.AddParameter("@IsCompleted", DbType.Boolean, isCompleted); command.AddParameter("@CompletedAtUtc", DbType.DateTimeOffset, isCompleted ? changedAtUtc : DBNull.Value); return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<bool> SwapPriorityAsync(Guid id, Guid swapWithId, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var transaction = await connection.BeginTransactionAsync(ct); await using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "DECLARE @First BIGINT = (SELECT PriorityPosition FROM dbo.SubjectGoals WHERE Id = @Id); DECLARE @Second BIGINT = (SELECT PriorityPosition FROM dbo.SubjectGoals WHERE Id = @SwapWithId); UPDATE dbo.SubjectGoals SET PriorityPosition = CASE WHEN Id = @Id THEN @Second WHEN Id = @SwapWithId THEN @First END WHERE Id IN (@Id, @SwapWithId);";
        command.AddParameter("@Id", DbType.Guid, id); command.AddParameter("@SwapWithId", DbType.Guid, swapWithId); var changed = await command.ExecuteNonQueryAsync(ct); await transaction.CommitAsync(ct); return changed == 2;
    }

    private static SubjectGoal ReadGoal(DbDataReader reader) => new(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetString(3), (GoalKind)reader.GetByte(4), reader.IsDBNull(5) ? null : reader.GetGuid(5), reader.IsDBNull(6) ? null : reader.GetDecimal(6), reader.IsDBNull(7) ? null : DateOnly.FromDateTime(reader.GetDateTime(7)), (GoalPeriod)reader.GetByte(8), reader.IsDBNull(9) ? null : DateOnly.FromDateTime(reader.GetDateTime(9)), reader.IsDBNull(10) ? null : DateOnly.FromDateTime(reader.GetDateTime(10)), reader.GetInt64(11), reader.GetBoolean(12), reader.IsDBNull(13) ? null : reader.GetFieldValue<DateTimeOffset>(13), reader.GetFieldValue<DateTimeOffset>(14));
}
