using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public interface ISubjectGoalActivityRepository
{
    Task<IReadOnlyCollection<SubjectGoal>> ListForPeriodAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
