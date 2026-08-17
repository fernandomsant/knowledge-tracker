using System.Data;
using System.Data.Common;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Data.Knowledge.Repositories;

public sealed class SqlServerSubjectGoalRepository(Func<DbConnection> connectionFactory) : ISubjectGoalRepository, ISubjectGoalActivityRepository
{
    public async Task<IReadOnlyCollection<SubjectGoal>> ListBySubjectAsync(Guid subjectId, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, SubjectId, TopicId, Title, GoalKind, MetricDefinitionId, TargetValue, TargetDate, GoalPeriod, CustomPeriodStartDate, CustomPeriodEndDate, PriorityPosition, IsCompleted, CompletedAtUtc, CreatedAtUtc, IsActive, DeactivatedAtUtc FROM dbo.SubjectGoals WHERE SubjectId = @SubjectId AND IsActive = 1 ORDER BY PriorityPosition;";
        command.AddParameter("@SubjectId", DbType.Guid, subjectId); await using var reader = await command.ExecuteReaderAsync(ct);
        var goals = new List<SubjectGoal>(); while (await reader.ReadAsync(ct)) goals.Add(ReadGoal(reader)); return goals;
    }

    public async Task<SubjectGoal?> FindAsync(Guid id, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, SubjectId, TopicId, Title, GoalKind, MetricDefinitionId, TargetValue, TargetDate, GoalPeriod, CustomPeriodStartDate, CustomPeriodEndDate, PriorityPosition, IsCompleted, CompletedAtUtc, CreatedAtUtc, IsActive, DeactivatedAtUtc FROM dbo.SubjectGoals WHERE Id = @Id AND IsActive = 1;";
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

    public async Task<bool> DeleteAsync(Guid id, DateTimeOffset deactivatedAtUtc, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand(); command.CommandText = "UPDATE dbo.SubjectGoals SET IsActive = 0, DeactivatedAtUtc = @DeactivatedAtUtc WHERE Id = @Id AND IsActive = 1;"; command.AddParameter("@Id", DbType.Guid, id); command.AddParameter("@DeactivatedAtUtc", DbType.DateTimeOffset, deactivatedAtUtc); return await command.ExecuteNonQueryAsync(ct) > 0;
    }
    public async Task<bool> CompleteAsync(Guid id, DateTimeOffset completedAtUtc, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand(); command.CommandText = "UPDATE dbo.SubjectGoals SET IsCompleted = 1, CompletedAtUtc = @CompletedAtUtc WHERE Id = @Id AND IsActive = 1 AND GoalKind = 2 AND IsCompleted = 0;"; command.AddParameter("@Id", DbType.Guid, id); command.AddParameter("@CompletedAtUtc", DbType.DateTimeOffset, completedAtUtc); return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<SubjectSubGoal?> FindSubGoalAsync(Guid id, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, SubjectGoalId, Title, IsCompleted, CompletedAtUtc, CreatedAtUtc FROM dbo.SubjectSubGoals WHERE Id = @Id;";
        command.AddParameter("@Id", DbType.Guid, id); await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct)
            ? new SubjectSubGoal(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetBoolean(3), reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4), reader.GetFieldValue<DateTimeOffset>(5))
            : null;
    }

    public async Task<IReadOnlyCollection<SubjectGoal>> ListForPeriodAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, SubjectId, TopicId, Title, GoalKind, MetricDefinitionId, TargetValue, TargetDate, GoalPeriod, CustomPeriodStartDate, CustomPeriodEndDate, PriorityPosition, IsCompleted, CompletedAtUtc, CreatedAtUtc, IsActive, DeactivatedAtUtc FROM dbo.SubjectGoals WHERE CONVERT(date, CreatedAtUtc) <= @To AND (IsActive = 1 OR CONVERT(date, DeactivatedAtUtc) >= @From) ORDER BY CreatedAtUtc, Id;";
        command.AddParameter("@From", DbType.Date, from); command.AddParameter("@To", DbType.Date, to);
        await using var reader = await command.ExecuteReaderAsync(ct); var goals = new List<SubjectGoal>();
        while (await reader.ReadAsync(ct)) goals.Add(ReadGoal(reader));
        return goals;
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

    public async Task<bool> SetSubGoalCompletionAsync(Guid id, bool isCompleted, DateTimeOffset changedAtUtc, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.SubjectSubGoals SET IsCompleted = @IsCompleted, CompletedAtUtc = @CompletedAtUtc WHERE Id = @Id AND EXISTS (SELECT 1 FROM dbo.SubjectGoals WHERE Id = SubjectGoalId AND IsActive = 1 AND GoalKind = 2);";
        command.AddParameter("@Id", DbType.Guid, id); command.AddParameter("@IsCompleted", DbType.Boolean, isCompleted); command.AddParameter("@CompletedAtUtc", DbType.DateTimeOffset, isCompleted ? changedAtUtc : DBNull.Value); return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<bool> SwapPriorityAsync(Guid id, Guid swapWithId, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var transaction = await connection.BeginTransactionAsync(ct); await using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "DECLARE @First BIGINT = (SELECT PriorityPosition FROM dbo.SubjectGoals WHERE Id = @Id AND IsActive = 1); DECLARE @Second BIGINT = (SELECT PriorityPosition FROM dbo.SubjectGoals WHERE Id = @SwapWithId AND IsActive = 1); UPDATE dbo.SubjectGoals SET PriorityPosition = CASE WHEN Id = @Id THEN @Second WHEN Id = @SwapWithId THEN @First END WHERE Id IN (@Id, @SwapWithId) AND IsActive = 1;";
        command.AddParameter("@Id", DbType.Guid, id); command.AddParameter("@SwapWithId", DbType.Guid, swapWithId); var changed = await command.ExecuteNonQueryAsync(ct); await transaction.CommitAsync(ct); return changed == 2;
    }

    private static SubjectGoal ReadGoal(DbDataReader reader) => new(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetString(3), (GoalKind)reader.GetByte(4), reader.IsDBNull(5) ? null : reader.GetGuid(5), reader.IsDBNull(6) ? null : reader.GetDecimal(6), reader.IsDBNull(7) ? null : DateOnly.FromDateTime(reader.GetDateTime(7)), (GoalPeriod)reader.GetByte(8), reader.IsDBNull(9) ? null : DateOnly.FromDateTime(reader.GetDateTime(9)), reader.IsDBNull(10) ? null : DateOnly.FromDateTime(reader.GetDateTime(10)), reader.GetInt64(11), reader.GetBoolean(12), reader.IsDBNull(13) ? null : reader.GetFieldValue<DateTimeOffset>(13), reader.GetFieldValue<DateTimeOffset>(14), reader.GetBoolean(15), reader.IsDBNull(16) ? null : reader.GetFieldValue<DateTimeOffset>(16));
}
