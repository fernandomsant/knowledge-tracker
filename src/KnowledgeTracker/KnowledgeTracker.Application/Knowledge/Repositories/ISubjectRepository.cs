using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public interface ISubjectRepository
{
    Task<Subject?> FindAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<Subject>> ListAsync(CancellationToken ct);
    Task<bool> HasChildrenAsync(Guid subjectId, CancellationToken ct);
    Task AddAsync(Subject subject, CancellationToken ct);
    Task UpdateAsync(Subject subject, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
