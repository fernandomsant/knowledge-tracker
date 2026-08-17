namespace KnowledgeTracker.Application.Knowledge;

public interface ISubjectGoalActivityService
{
    Task<IReadOnlyCollection<GoalActivityDetails>> GetAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task ReevaluateMetricGoalsAsync(Guid subjectId, CancellationToken ct, DateOnly? affectedDate = null);
}
