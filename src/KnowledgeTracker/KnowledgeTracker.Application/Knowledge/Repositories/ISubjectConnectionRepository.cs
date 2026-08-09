using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public interface ISubjectConnectionRepository
{
    Task<SubjectConnection?> FindAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsAsync(Guid subjectId, Guid connectedSubjectId, CancellationToken ct);
    Task<IReadOnlyCollection<SubjectConnection>> ListBySubjectAsync(Guid subjectId, CancellationToken ct);
    Task AddAsync(SubjectConnection connection, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
