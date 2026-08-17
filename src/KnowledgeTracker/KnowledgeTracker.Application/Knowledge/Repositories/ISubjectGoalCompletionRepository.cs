using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public interface ISubjectGoalCompletionRepository
{
    Task<IReadOnlyCollection<SubjectGoalCompletion>> ListAsync(IReadOnlyCollection<Guid> goalIds, DateOnly from, DateOnly to, CancellationToken ct);
    Task RegisterAsync(SubjectGoalCompletion completion, CancellationToken ct);
    Task RemoveAsync(Guid goalId, DateOnly occurrenceStartDate, DateOnly occurrenceEndDate, CancellationToken ct);
}
