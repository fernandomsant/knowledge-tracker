using System.Data;
using System.Data.Common;
using System.Text.Json;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Data.Knowledge.Repositories;

public sealed class SqlServerSubjectLayoutRepository(Func<DbConnection> connectionFactory) : ISubjectLayoutRepository
{
    public async Task<IReadOnlyCollection<SubjectLayoutPosition>> ListAsync(CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT SubjectId, NormalizedX, NormalizedY FROM dbo.SubjectLayout;";
        await using var reader = await command.ExecuteReaderAsync(ct);

        var positions = new List<SubjectLayoutPosition>();
        while (await reader.ReadAsync(ct))
            positions.Add(new SubjectLayoutPosition(reader.GetGuid(0), reader.GetDecimal(1), reader.GetDecimal(2)));
        return positions;
    }

    public async Task UpsertAsync(IReadOnlyCollection<SubjectLayoutPosition> positions, CancellationToken ct)
    {
        if (positions.Count == 0)
            return;

        var payload = JsonSerializer.Serialize(positions.Select(position => new
        {
            position.SubjectId,
            position.NormalizedX,
            position.NormalizedY
        }));

        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DECLARE @Source TABLE
            (
                SubjectId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                NormalizedX DECIMAL(9,8) NOT NULL,
                NormalizedY DECIMAL(9,8) NOT NULL
            );

            INSERT INTO @Source (SubjectId, NormalizedX, NormalizedY)
            SELECT SubjectId, NormalizedX, NormalizedY
            FROM OPENJSON(@Positions)
            WITH
            (
                SubjectId UNIQUEIDENTIFIER '$.SubjectId',
                NormalizedX DECIMAL(9,8) '$.NormalizedX',
                NormalizedY DECIMAL(9,8) '$.NormalizedY'
            );

            UPDATE target
            SET NormalizedX = source.NormalizedX,
                NormalizedY = source.NormalizedY,
                UpdatedAtUtc = SYSUTCDATETIME()
            FROM dbo.SubjectLayout target
            JOIN @Source source ON source.SubjectId = target.SubjectId;

            INSERT INTO dbo.SubjectLayout (SubjectId, NormalizedX, NormalizedY, UpdatedAtUtc)
            SELECT source.SubjectId, source.NormalizedX, source.NormalizedY, SYSUTCDATETIME()
            FROM @Source source
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM dbo.SubjectLayout target
                WHERE target.SubjectId = source.SubjectId
            );
            """;
        command.AddParameter("@Positions", DbType.String, payload);
        await command.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
