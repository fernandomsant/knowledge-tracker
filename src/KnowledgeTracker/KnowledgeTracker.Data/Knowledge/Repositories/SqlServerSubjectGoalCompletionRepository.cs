using System.Data;
using System.Data.Common;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Data.Knowledge.Repositories;

public sealed class SqlServerSubjectGoalCompletionRepository(Func<DbConnection> connectionFactory, CurrentWorkspaceDataScope dataScope) : ISubjectGoalCompletionRepository
{
    public async Task<IReadOnlyCollection<SubjectGoalCompletion>> ListAsync(IReadOnlyCollection<Guid> goalIds, DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (goalIds.Count == 0) return [];
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        var parameters = goalIds.Select((id, index) => { var name = $"@Goal{index}"; command.AddParameter(name, DbType.Guid, id); return name; }).ToArray();
        command.CommandText = $"SELECT completion.Id, completion.SubjectGoalId, completion.OccurrenceStartDate, completion.OccurrenceEndDate, completion.CompletedAtUtc, completion.CompletionSource FROM dbo.SubjectGoalCompletions AS completion INNER JOIN dbo.SubjectGoals AS goal ON goal.Id = completion.SubjectGoalId INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE completion.SubjectGoalId IN ({string.Join(',', parameters)}) AND subject.UserId = @UserId AND subject.WorkspaceId = @WorkspaceId AND completion.OccurrenceStartDate <= @To AND completion.OccurrenceEndDate >= @From;";
        command.AddParameter("@From", DbType.Date, from); command.AddParameter("@To", DbType.Date, to);
        AddScope(command);
        await using var reader = await command.ExecuteReaderAsync(ct); var result = new List<SubjectGoalCompletion>();
        while (await reader.ReadAsync(ct)) result.Add(new SubjectGoalCompletion(reader.GetGuid(0), reader.GetGuid(1), DateOnly.FromDateTime(reader.GetDateTime(2)), DateOnly.FromDateTime(reader.GetDateTime(3)), reader.GetFieldValue<DateTimeOffset>(4), (GoalCompletionSource)reader.GetByte(5)));
        return result;
    }

    public async Task RegisterAsync(SubjectGoalCompletion completion, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "MERGE dbo.SubjectGoalCompletions WITH (HOLDLOCK) AS target USING (SELECT @Id AS Id, @SubjectGoalId AS SubjectGoalId, @OccurrenceStartDate AS OccurrenceStartDate, @OccurrenceEndDate AS OccurrenceEndDate, @CompletedAtUtc AS CompletedAtUtc, @CompletionSource AS CompletionSource) AS source ON target.SubjectGoalId = source.SubjectGoalId AND target.OccurrenceStartDate = source.OccurrenceStartDate AND target.OccurrenceEndDate = source.OccurrenceEndDate WHEN MATCHED AND EXISTS (SELECT 1 FROM dbo.SubjectGoals AS goal INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE goal.Id = target.SubjectGoalId AND subject.UserId = @UserId AND subject.WorkspaceId = @WorkspaceId) THEN UPDATE SET CompletedAtUtc = CASE WHEN target.CompletedAtUtc <= source.CompletedAtUtc THEN target.CompletedAtUtc ELSE source.CompletedAtUtc END, CompletionSource = source.CompletionSource WHEN NOT MATCHED AND EXISTS (SELECT 1 FROM dbo.SubjectGoals AS goal INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE goal.Id = source.SubjectGoalId AND subject.UserId = @UserId AND subject.WorkspaceId = @WorkspaceId) THEN INSERT (Id, SubjectGoalId, OccurrenceStartDate, OccurrenceEndDate, CompletedAtUtc, CompletionSource) VALUES (source.Id, source.SubjectGoalId, source.OccurrenceStartDate, source.OccurrenceEndDate, source.CompletedAtUtc, source.CompletionSource);";
        command.AddParameter("@Id", DbType.Guid, completion.Id); command.AddParameter("@SubjectGoalId", DbType.Guid, completion.SubjectGoalId); command.AddParameter("@OccurrenceStartDate", DbType.Date, completion.OccurrenceStartDate); command.AddParameter("@OccurrenceEndDate", DbType.Date, completion.OccurrenceEndDate); command.AddParameter("@CompletedAtUtc", DbType.DateTimeOffset, completion.CompletedAtUtc); command.AddParameter("@CompletionSource", DbType.Byte, (byte)completion.Source); AddScope(command); await command.ExecuteNonQueryAsync(ct);
    }

    public async Task RemoveAsync(Guid goalId, DateOnly occurrenceStartDate, DateOnly occurrenceEndDate, CancellationToken ct)
    {
        await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand();
        command.CommandText = "DELETE completion FROM dbo.SubjectGoalCompletions AS completion INNER JOIN dbo.SubjectGoals AS goal ON goal.Id = completion.SubjectGoalId INNER JOIN dbo.Subjects AS subject ON subject.Id = goal.SubjectId WHERE completion.SubjectGoalId = @SubjectGoalId AND subject.UserId = @UserId AND subject.WorkspaceId = @WorkspaceId AND completion.OccurrenceStartDate = @OccurrenceStartDate AND completion.OccurrenceEndDate = @OccurrenceEndDate;";
        command.AddParameter("@SubjectGoalId", DbType.Guid, goalId); command.AddParameter("@OccurrenceStartDate", DbType.Date, occurrenceStartDate); command.AddParameter("@OccurrenceEndDate", DbType.Date, occurrenceEndDate); AddScope(command); await command.ExecuteNonQueryAsync(ct);
    }

    private void AddScope(DbCommand command)
    {
        command.AddParameter("@UserId", DbType.Guid, dataScope.RequireUserId());
        command.AddParameter("@WorkspaceId", DbType.Guid, dataScope.RequireWorkspaceId());
    }
}
