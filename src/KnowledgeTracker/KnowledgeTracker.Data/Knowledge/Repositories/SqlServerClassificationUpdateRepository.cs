using System.Data;
using System.Data.Common;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Database;

namespace KnowledgeTracker.Data.Knowledge.Repositories;

public sealed class SqlServerClassificationUpdateRepository(Func<DbConnection> connectionFactory)
    : IClassificationUpdateRepository
{
    public async Task<IReadOnlyCollection<ClassificationUpdate>> ListAfterAsync(
        ClassificationUpdateCheckpoint checkpoint,
        int take,
        CancellationToken ct
    )
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Take) Id, NoteId, CompletedAtUtc
            FROM dbo.ClassificationJobs
            WHERE Status IN (3, 4)
              AND CompletedAtUtc IS NOT NULL
              AND
              (
                  CompletedAtUtc > @CompletedAtUtc
                  OR (CompletedAtUtc = @CompletedAtUtc AND Id > @JobId)
              )
            ORDER BY CompletedAtUtc, Id;
            """;
        command.AddParameter("@Take", DbType.Int32, take);
        command.AddParameter("@CompletedAtUtc", DbType.DateTimeOffset, checkpoint.CompletedAtUtc);
        command.AddParameter("@JobId", DbType.Guid, checkpoint.JobId);

        var updates = new List<ClassificationUpdate>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            updates.Add(new ClassificationUpdate(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetFieldValue<DateTimeOffset>(2)
            ));

        return updates;
    }
}
