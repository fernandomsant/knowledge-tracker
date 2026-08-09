namespace KnowledgeTracker.Application.Knowledge;

public interface ISubjectService
{
    Task<SubjectDetails?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<SubjectSummary>> ListAsync(CancellationToken ct);
    Task<SubjectSummary> CreateAsync(CreateSubjectRequest request, CancellationToken ct);
    Task<SubjectSummary?> UpdateAsync(Guid id, UpdateSubjectRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
