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
        command.CommandText = "SELECT Id, SubjectId, Title, GoalKind, MetricDefinitionId, TargetValue, TargetDate, CreatedAtUtc FROM dbo.SubjectGoals WHERE SubjectId = @SubjectId ORDER BY CreatedAtUtc DESC;";
        command.AddParameter("@SubjectId", DbType.Guid, subjectId); await using var reader = await command.ExecuteReaderAsync(ct);
        var goals = new List<SubjectGoal>(); while (await reader.ReadAsync(ct)) goals.Add(ReadGoal(reader)); return goals;
    }

    public async Task AddAsync(SubjectGoal goal, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO dbo.SubjectGoals (Id, SubjectId, Title, GoalKind, MetricDefinitionId, TargetValue, TargetDate, CreatedAtUtc) VALUES (@Id, @SubjectId, @Title, @GoalKind, @MetricDefinitionId, @TargetValue, @TargetDate, @CreatedAtUtc);";
        command.AddParameter("@Id", DbType.Guid, goal.Id); command.AddParameter("@SubjectId", DbType.Guid, goal.SubjectId); command.AddParameter("@Title", DbType.String, goal.Title); command.AddParameter("@GoalKind", DbType.Byte, (byte)goal.Kind); command.AddParameter("@MetricDefinitionId", DbType.Guid, (object?)goal.MetricDefinitionId ?? DBNull.Value); command.AddParameter("@TargetValue", DbType.Decimal, (object?)goal.TargetValue ?? DBNull.Value); command.AddParameter("@TargetDate", DbType.Date, (object?)goal.TargetDate ?? DBNull.Value); command.AddParameter("@CreatedAtUtc", DbType.DateTimeOffset, goal.CreatedAtUtc); await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand(); command.CommandText = "DELETE FROM dbo.SubjectGoals WHERE Id = @Id;"; command.AddParameter("@Id", DbType.Guid, id); return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    private static SubjectGoal ReadGoal(DbDataReader reader) => new(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), (GoalKind)reader.GetByte(3), reader.IsDBNull(4) ? null : reader.GetGuid(4), reader.IsDBNull(5) ? null : reader.GetDecimal(5), reader.IsDBNull(6) ? null : DateOnly.FromDateTime(reader.GetDateTime(6)), reader.GetFieldValue<DateTimeOffset>(7));
}
