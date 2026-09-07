using System.Data;
using System.Data.Common;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Data.Knowledge.Repositories;

public sealed class SqlServerSubjectGoalRepository(
    Func<DbConnection> connectionFactory,
    CurrentUserDataScope dataScope) : ISubjectGoalRepository, ISubjectGoalActivityRepository
{
    private const string GoalColumns = "goal.Id, goal.SubjectId, goal.TopicId, goal.Title, goal.GoalKind, goal.MetricDefinitionId, goal.TargetValue, goal.TargetDate, goal.GoalPeriod, goal.CustomPeriodStartDate, goal.CustomPeriodEndDate, goal.PriorityPosition, goal.IsCompleted, goal.CompletedAtUtc, goal.CreatedAtUtc, goal.IsActive, goal.DeactivatedAtUtc";

    public async Task<IReadOnlyCollection<SubjectGoal>> ListBySubjectAsync(Guid subjectId, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {GoalColumns} FROM dbo.SubjectGoals AS goal INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE goal.SubjectId = @SubjectId AND subject.UserId = @UserId AND goal.IsActive = 1 ORDER BY goal.PriorityPosition;";
        AddUser(command, subjectId);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var goals = new List<SubjectGoal>(); while (await reader.ReadAsync(ct)) goals.Add(ReadGoal(reader)); return goals;
    }

    public async Task<SubjectGoal?> FindAsync(Guid id, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {GoalColumns} FROM dbo.SubjectGoals AS goal INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE goal.Id = @Id AND subject.UserId = @UserId AND goal.IsActive = 1;";
        command.AddParameter("@Id", DbType.Guid, id); command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId());
        await using var reader = await command.ExecuteReaderAsync(ct); return await reader.ReadAsync(ct) ? ReadGoal(reader) : null;
    }

    public async Task AddAsync(SubjectGoal goal, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO dbo.SubjectGoals (Id, SubjectId, TopicId, Title, GoalKind, MetricDefinitionId, TargetValue, TargetDate, GoalPeriod, CustomPeriodStartDate, CustomPeriodEndDate, PriorityPosition, IsCompleted, CompletedAtUtc, CreatedAtUtc) SELECT @Id, @SubjectId, @TopicId, @Title, @GoalKind, @MetricDefinitionId, @TargetValue, @TargetDate, @GoalPeriod, @CustomPeriodStartDate, @CustomPeriodEndDate, (SELECT ISNULL(MAX(goal.PriorityPosition), 0) + 1 FROM dbo.SubjectGoals AS goal WITH (TABLOCKX) INNER JOIN dbo.Subjects AS owner ON owner.Id = goal.SubjectId WHERE owner.UserId = @UserId), @IsCompleted, @CompletedAtUtc, @CreatedAtUtc WHERE EXISTS (SELECT 1 FROM dbo.Subjects WHERE Id = @SubjectId AND UserId = @UserId);";
        AddGoalParameters(command, goal); command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId()); await command.ExecuteNonQueryAsync(ct);
    }

    public async Task UpdateAsync(SubjectGoal goal, IReadOnlyCollection<SubjectSubGoal> subGoals, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var transaction = await connection.BeginTransactionAsync(ct);
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "UPDATE goal SET TopicId = @TopicId, Title = @Title, GoalKind = @GoalKind, MetricDefinitionId = @MetricDefinitionId, TargetValue = @TargetValue, TargetDate = @TargetDate, GoalPeriod = @GoalPeriod, CustomPeriodStartDate = @CustomPeriodStartDate, CustomPeriodEndDate = @CustomPeriodEndDate, IsCompleted = @IsCompleted, CompletedAtUtc = @CompletedAtUtc FROM dbo.SubjectGoals AS goal INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE goal.Id = @Id AND subject.UserId = @UserId; DELETE subGoal FROM dbo.SubjectSubGoals AS subGoal WHERE subGoal.SubjectGoalId = @Id AND EXISTS (SELECT 1 FROM dbo.SubjectGoals AS goal INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE goal.Id = @Id AND subject.UserId = @UserId);";
            AddGoalParameters(command, goal); command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId()); await command.ExecuteNonQueryAsync(ct);
        }
        foreach (var subGoal in subGoals)
        {
            await using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "INSERT INTO dbo.SubjectSubGoals (Id, SubjectGoalId, Title, IsCompleted, CompletedAtUtc, CreatedAtUtc) SELECT @Id, @SubjectGoalId, @Title, @IsCompleted, @CompletedAtUtc, @CreatedAtUtc WHERE EXISTS (SELECT 1 FROM dbo.SubjectGoals AS goal INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE goal.Id = @SubjectGoalId AND subject.UserId = @UserId);";
            AddSubGoalParameters(command, subGoal); command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId()); await command.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, DateTimeOffset deactivatedAtUtc, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE goal SET IsActive = 0, DeactivatedAtUtc = @DeactivatedAtUtc FROM dbo.SubjectGoals AS goal INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE goal.Id = @Id AND subject.UserId = @UserId AND goal.IsActive = 1;";
        command.AddParameter("@Id", DbType.Guid, id); command.AddParameter("@DeactivatedAtUtc", DbType.DateTimeOffset, deactivatedAtUtc); command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId()); return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<bool> CompleteAsync(Guid id, DateTimeOffset completedAtUtc, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE goal SET IsCompleted = 1, CompletedAtUtc = @CompletedAtUtc FROM dbo.SubjectGoals AS goal INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE goal.Id = @Id AND subject.UserId = @UserId AND goal.IsActive = 1 AND goal.GoalKind = 2 AND goal.IsCompleted = 0;";
        command.AddParameter("@Id", DbType.Guid, id); command.AddParameter("@CompletedAtUtc", DbType.DateTimeOffset, completedAtUtc); command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId()); return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<SubjectSubGoal?> FindSubGoalAsync(Guid id, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT subGoal.Id, subGoal.SubjectGoalId, subGoal.Title, subGoal.IsCompleted, subGoal.CompletedAtUtc, subGoal.CreatedAtUtc FROM dbo.SubjectSubGoals AS subGoal INNER JOIN dbo.SubjectGoals AS goal ON goal.Id = subGoal.SubjectGoalId INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE subGoal.Id = @Id AND subject.UserId = @UserId;";
        command.AddParameter("@Id", DbType.Guid, id); command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId()); await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadSubGoal(reader) : null;
    }

    public async Task<IReadOnlyCollection<SubjectGoal>> ListForPeriodAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {GoalColumns} FROM dbo.SubjectGoals AS goal INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE subject.UserId = @UserId AND CONVERT(date, goal.CreatedAtUtc) <= @To AND (goal.IsActive = 1 OR CONVERT(date, goal.DeactivatedAtUtc) >= @From) ORDER BY goal.CreatedAtUtc, goal.Id;";
        command.AddParameter("@From", DbType.Date, from); command.AddParameter("@To", DbType.Date, to); command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId()); await using var reader = await command.ExecuteReaderAsync(ct);
        var goals = new List<SubjectGoal>(); while (await reader.ReadAsync(ct)) goals.Add(ReadGoal(reader)); return goals;
    }

    public async Task AddSubGoalsAsync(IReadOnlyCollection<SubjectSubGoal> subGoals, CancellationToken ct)
    {
        if (subGoals.Count == 0) return; await using var connection = connectionFactory(); await connection.OpenAsync(ct);
        foreach (var subGoal in subGoals)
        {
            await using var command = connection.CreateCommand(); command.CommandText = "INSERT INTO dbo.SubjectSubGoals (Id, SubjectGoalId, Title, IsCompleted, CompletedAtUtc, CreatedAtUtc) SELECT @Id, @SubjectGoalId, @Title, @IsCompleted, @CompletedAtUtc, @CreatedAtUtc WHERE EXISTS (SELECT 1 FROM dbo.SubjectGoals AS goal INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE goal.Id = @SubjectGoalId AND subject.UserId = @UserId);";
            AddSubGoalParameters(command, subGoal); command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId()); await command.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task<IReadOnlyCollection<SubjectSubGoal>> ListSubGoalsAsync(IReadOnlyCollection<Guid> subjectGoalIds, CancellationToken ct)
    {
        if (subjectGoalIds.Count == 0) return []; await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        var parameters = subjectGoalIds.Select((id, index) => { var name = $"@Id{index}"; command.AddParameter(name, DbType.Guid, id); return name; });
        command.CommandText = $"SELECT subGoal.Id, subGoal.SubjectGoalId, subGoal.Title, subGoal.IsCompleted, subGoal.CompletedAtUtc, subGoal.CreatedAtUtc FROM dbo.SubjectSubGoals AS subGoal INNER JOIN dbo.SubjectGoals AS goal ON goal.Id = subGoal.SubjectGoalId INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE subGoal.SubjectGoalId IN ({string.Join(',', parameters)}) AND subject.UserId = @UserId ORDER BY subGoal.CreatedAtUtc;";
        command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId()); await using var reader = await command.ExecuteReaderAsync(ct); var result = new List<SubjectSubGoal>(); while (await reader.ReadAsync(ct)) result.Add(ReadSubGoal(reader)); return result;
    }

    public async Task<bool> SetSubGoalCompletionAsync(Guid id, bool isCompleted, DateTimeOffset changedAtUtc, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE subGoal SET IsCompleted = @IsCompleted, CompletedAtUtc = @CompletedAtUtc FROM dbo.SubjectSubGoals AS subGoal INNER JOIN dbo.SubjectGoals AS goal ON goal.Id = subGoal.SubjectGoalId INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE subGoal.Id = @Id AND subject.UserId = @UserId AND goal.IsActive = 1 AND goal.GoalKind = 2;";
        command.AddParameter("@Id", DbType.Guid, id); command.AddParameter("@IsCompleted", DbType.Boolean, isCompleted); command.AddParameter("@CompletedAtUtc", DbType.DateTimeOffset, isCompleted ? changedAtUtc : DBNull.Value); command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId()); return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<bool> SwapPriorityAsync(Guid id, Guid swapWithId, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var transaction = await connection.BeginTransactionAsync(ct); await using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "DECLARE @First BIGINT = (SELECT goal.PriorityPosition FROM dbo.SubjectGoals AS goal INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE goal.Id = @Id AND subject.UserId = @UserId AND goal.IsActive = 1); DECLARE @Second BIGINT = (SELECT goal.PriorityPosition FROM dbo.SubjectGoals AS goal INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE goal.Id = @SwapWithId AND subject.UserId = @UserId AND goal.IsActive = 1); UPDATE goal SET PriorityPosition = CASE WHEN goal.Id = @Id THEN @Second WHEN goal.Id = @SwapWithId THEN @First END FROM dbo.SubjectGoals AS goal INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE goal.Id IN (@Id, @SwapWithId) AND subject.UserId = @UserId AND goal.IsActive = 1;";
        command.AddParameter("@Id", DbType.Guid, id); command.AddParameter("@SwapWithId", DbType.Guid, swapWithId); command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId()); var changed = await command.ExecuteNonQueryAsync(ct); await transaction.CommitAsync(ct); return changed == 2;
    }

    private void AddUser(DbCommand command, Guid subjectId)
    {
        command.AddParameter("@SubjectId", DbType.Guid, subjectId);
        command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId());
    }

    private static void AddGoalParameters(DbCommand command, SubjectGoal goal)
    {
        command.AddParameter("@Id", DbType.Guid, goal.Id); command.AddParameter("@SubjectId", DbType.Guid, goal.SubjectId); command.AddParameter("@TopicId", DbType.Guid, goal.TopicId); command.AddParameter("@Title", DbType.String, goal.Title); command.AddParameter("@GoalKind", DbType.Byte, (byte)goal.Kind); command.AddParameter("@MetricDefinitionId", DbType.Guid, (object?)goal.MetricDefinitionId ?? DBNull.Value); command.AddParameter("@TargetValue", DbType.Decimal, (object?)goal.TargetValue ?? DBNull.Value); command.AddParameter("@TargetDate", DbType.Date, (object?)goal.TargetDate ?? DBNull.Value); command.AddParameter("@GoalPeriod", DbType.Byte, (byte)goal.Period); command.AddParameter("@CustomPeriodStartDate", DbType.Date, (object?)goal.CustomPeriodStartDate ?? DBNull.Value); command.AddParameter("@CustomPeriodEndDate", DbType.Date, (object?)goal.CustomPeriodEndDate ?? DBNull.Value); command.AddParameter("@IsCompleted", DbType.Boolean, goal.IsCompleted); command.AddParameter("@CompletedAtUtc", DbType.DateTimeOffset, (object?)goal.CompletedAtUtc ?? DBNull.Value); command.AddParameter("@CreatedAtUtc", DbType.DateTimeOffset, goal.CreatedAtUtc);
    }

    private static void AddSubGoalParameters(DbCommand command, SubjectSubGoal subGoal)
    {
        command.AddParameter("@Id", DbType.Guid, subGoal.Id); command.AddParameter("@SubjectGoalId", DbType.Guid, subGoal.SubjectGoalId); command.AddParameter("@Title", DbType.String, subGoal.Title); command.AddParameter("@IsCompleted", DbType.Boolean, subGoal.IsCompleted); command.AddParameter("@CompletedAtUtc", DbType.DateTimeOffset, (object?)subGoal.CompletedAtUtc ?? DBNull.Value); command.AddParameter("@CreatedAtUtc", DbType.DateTimeOffset, subGoal.CreatedAtUtc);
    }

    private static SubjectGoal ReadGoal(DbDataReader reader) => new(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetString(3), (GoalKind)reader.GetByte(4), reader.IsDBNull(5) ? null : reader.GetGuid(5), reader.IsDBNull(6) ? null : reader.GetDecimal(6), reader.IsDBNull(7) ? null : DateOnly.FromDateTime(reader.GetDateTime(7)), (GoalPeriod)reader.GetByte(8), reader.IsDBNull(9) ? null : DateOnly.FromDateTime(reader.GetDateTime(9)), reader.IsDBNull(10) ? null : DateOnly.FromDateTime(reader.GetDateTime(10)), reader.GetInt64(11), reader.GetBoolean(12), reader.IsDBNull(13) ? null : reader.GetFieldValue<DateTimeOffset>(13), reader.GetFieldValue<DateTimeOffset>(14), reader.GetBoolean(15), reader.IsDBNull(16) ? null : reader.GetFieldValue<DateTimeOffset>(16));
    private static SubjectSubGoal ReadSubGoal(DbDataReader reader) => new(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetBoolean(3), reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4), reader.GetFieldValue<DateTimeOffset>(5));
}
