using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public interface ISubjectGoalRepository
{
    Task<IReadOnlyCollection<SubjectGoal>> ListBySubjectAsync(Guid subjectId, CancellationToken ct);
    Task AddAsync(SubjectGoal goal, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
    Task<bool> CompleteAsync(Guid id, DateTimeOffset completedAtUtc, CancellationToken ct);
    Task AddSubGoalsAsync(IReadOnlyCollection<SubjectSubGoal> subGoals, CancellationToken ct);
    Task<IReadOnlyCollection<SubjectSubGoal>> ListSubGoalsAsync(IReadOnlyCollection<Guid> subjectGoalIds, CancellationToken ct);
    Task<bool> SetSubGoalCompletionAsync(Guid id, bool isCompleted, DateTimeOffset changedAtUtc, CancellationToken ct);
}
