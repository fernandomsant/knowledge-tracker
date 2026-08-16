namespace KnowledgeTracker.Application.Knowledge;

public interface ISubjectGoalService
{
    Task<IReadOnlyCollection<SubjectGoalDetails>> ListBySubjectAsync(Guid subjectId, CancellationToken ct);
    Task<SubjectGoalDetails?> CreateAsync(Guid subjectId, CreateSubjectGoalRequest request, CancellationToken ct);
    Task<SubjectGoalDetails?> UpdateAsync(Guid id, UpdateSubjectGoalRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    Task<bool> CompleteAsync(Guid id, CancellationToken ct);
    Task<bool> SetSubGoalCompletionAsync(Guid id, bool isCompleted, CancellationToken ct);
    Task<bool> SwapPriorityAsync(Guid id, Guid swapWithId, CancellationToken ct);
}
