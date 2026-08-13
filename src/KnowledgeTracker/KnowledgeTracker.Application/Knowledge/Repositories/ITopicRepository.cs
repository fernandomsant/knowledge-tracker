using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public interface ITopicRepository
{
    Task<Topic?> FindAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<Topic>> ListAsync(CancellationToken ct);
    Task AddAsync(Topic topic, CancellationToken ct);
    Task UpdateAsync(Topic topic, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
