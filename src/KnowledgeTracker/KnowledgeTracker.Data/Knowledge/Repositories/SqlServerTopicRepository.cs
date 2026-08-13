using System.Data;
using System.Data.Common;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Data.Database;
using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Data.Knowledge.Repositories;

public sealed class SqlServerTopicRepository(Func<DbConnection> connectionFactory) : ITopicRepository
{
    public async Task<Topic?> FindAsync(Guid id, CancellationToken ct) { await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand(); command.CommandText = "SELECT Id, Name FROM dbo.Topics WHERE Id = @Id;"; command.AddParameter("@Id", DbType.Guid, id); await using var reader = await command.ExecuteReaderAsync(ct); return await reader.ReadAsync(ct) ? new(reader.GetGuid(0), reader.GetString(1)) : null; }
    public async Task<IReadOnlyCollection<Topic>> ListAsync(CancellationToken ct) { await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand(); command.CommandText = "SELECT Id, Name FROM dbo.Topics ORDER BY Name, Id;"; await using var reader = await command.ExecuteReaderAsync(ct); var result = new List<Topic>(); while (await reader.ReadAsync(ct)) result.Add(new(reader.GetGuid(0), reader.GetString(1))); return result; }
    public async Task AddAsync(Topic topic, CancellationToken ct) { await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand(); command.CommandText = "INSERT INTO dbo.Topics (Id, Name) VALUES (@Id, @Name);"; Add(command, topic); await command.ExecuteNonQueryAsync(ct); }
    public async Task UpdateAsync(Topic topic, CancellationToken ct) { await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand(); command.CommandText = "UPDATE dbo.Topics SET Name = @Name WHERE Id = @Id;"; Add(command, topic); await command.ExecuteNonQueryAsync(ct); }
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct) { await using var connection = connectionFactory(); await connection.OpenAsync(ct); await using var command = connection.CreateCommand(); command.CommandText = "DELETE FROM dbo.Topics WHERE Id = @Id;"; command.AddParameter("@Id", DbType.Guid, id); return await command.ExecuteNonQueryAsync(ct) > 0; }
    private static void Add(DbCommand command, Topic topic) { command.AddParameter("@Id", DbType.Guid, topic.Id); command.AddParameter("@Name", DbType.String, topic.Name); }
}
