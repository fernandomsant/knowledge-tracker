using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public interface ISubjectLayoutRepository
{
    Task<IReadOnlyCollection<SubjectLayoutPosition>> ListAsync(CancellationToken ct);
    Task UpsertAsync(IReadOnlyCollection<SubjectLayoutPosition> positions, CancellationToken ct);
}
