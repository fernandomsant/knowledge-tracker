using System.Data;
using System.Data.Common;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Data.Knowledge.Repositories;

public sealed class SqlServerStudyMetricDefinitionRepository(Func<DbConnection> connectionFactory)
    : IStudyMetricDefinitionRepository
{
    public async Task<StudyMetricDefinition?> FindAsync(Guid id, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = CreateSelectCommand(connection, "WHERE Id = @Id;");
        command.AddParameter("@Id", DbType.Guid, id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadDefinition(reader) : null;
    }

    public async Task<StudyMetricDefinition?> FindByNormalizedNameAsync(string normalizedName, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = CreateSelectCommand(connection, "WHERE NormalizedName = @NormalizedName;");
        command.AddParameter("@NormalizedName", DbType.String, normalizedName);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadDefinition(reader) : null;
    }

    public async Task<IReadOnlyCollection<StudyMetricDefinition>> ListAsync(CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = CreateSelectCommand(connection, "ORDER BY Name, Id;");
        await using var reader = await command.ExecuteReaderAsync(ct);
        var definitions = new List<StudyMetricDefinition>();
        while (await reader.ReadAsync(ct)) definitions.Add(ReadDefinition(reader));
        return definitions;
    }

    public async Task AddAsync(StudyMetricDefinition definition, CancellationToken ct)
    {
        await using var connection = connectionFactory();
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.StudyMetricDefinitions (Id, Name, NormalizedName, NumberKind)
            VALUES (@Id, @Name, @NormalizedName, @NumberKind);
            """;
        command.AddParameter("@Id", DbType.Guid, definition.Id);
        command.AddParameter("@Name", DbType.String, definition.Name);
        command.AddParameter("@NormalizedName", DbType.String, definition.NormalizedName);
        command.AddParameter("@NumberKind", DbType.Byte, (byte)definition.NumberKind);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static DbCommand CreateSelectCommand(DbConnection connection, string suffix)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT Id, Name, NumberKind FROM dbo.StudyMetricDefinitions {suffix}";
        return command;
    }

    private static StudyMetricDefinition ReadDefinition(DbDataReader reader) =>
        new(reader.GetGuid(0), reader.GetString(1), (MetricNumberKind)reader.GetByte(2));
}
