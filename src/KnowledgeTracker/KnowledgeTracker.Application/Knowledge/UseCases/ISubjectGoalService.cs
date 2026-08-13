namespace KnowledgeTracker.Application.Knowledge;

public interface ISubjectGoalService
{
    Task<IReadOnlyCollection<SubjectGoalDetails>> ListBySubjectAsync(Guid subjectId, CancellationToken ct);
    Task<SubjectGoalDetails?> CreateAsync(Guid subjectId, CreateSubjectGoalRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    Task<bool> CompleteAsync(Guid id, CancellationToken ct);
}
