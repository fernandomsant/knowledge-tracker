using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public interface ISubjectGoalRepository
{
    Task<IReadOnlyCollection<SubjectGoal>> ListBySubjectAsync(Guid subjectId, CancellationToken ct);
    Task AddAsync(SubjectGoal goal, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
