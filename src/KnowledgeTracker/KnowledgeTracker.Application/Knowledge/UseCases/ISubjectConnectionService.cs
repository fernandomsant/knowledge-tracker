namespace KnowledgeTracker.Application.Knowledge;

public interface ISubjectConnectionService
{
    Task<IReadOnlyCollection<SubjectConnectionDetails>> ListBySubjectAsync(
        Guid subjectId,
        CancellationToken ct
    );

    Task<SubjectConnectionDetails?> CreateAsync(
        CreateSubjectConnectionRequest request,
        CancellationToken ct
    );

    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
