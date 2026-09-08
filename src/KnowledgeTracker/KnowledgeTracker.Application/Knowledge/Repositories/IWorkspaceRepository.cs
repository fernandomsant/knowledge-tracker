using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

// Application owns this persistence abstraction. Implementations in Data must
// apply the supplied user scope to every query and write.
public interface IWorkspaceRepository
{
    Task<IReadOnlyCollection<Workspace>> ListAsync(Guid userId, CancellationToken ct);
    Task<Workspace?> FindAsync(Guid id, Guid userId, CancellationToken ct);
    Task<int> CountAsync(Guid userId, CancellationToken ct);
    Task AddAsync(Workspace workspace, CancellationToken ct);
}
