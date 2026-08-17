using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed record CreateSubjectGoalRequest(Guid TopicId, string Title, GoalKind Kind, Guid? MetricDefinitionId, decimal? TargetValue, DateOnly? TargetDate, GoalPeriod Period, DateOnly? PeriodStartDate, DateOnly? PeriodEndDate, IReadOnlyCollection<string> SubGoals);

public sealed record UpdateSubjectGoalRequest(Guid TopicId, string Title, GoalKind Kind, Guid? MetricDefinitionId, decimal? TargetValue, DateOnly? TargetDate, GoalPeriod Period, DateOnly? PeriodStartDate, DateOnly? PeriodEndDate, IReadOnlyCollection<string> SubGoals);

public sealed record SubjectSubGoalDetails(Guid Id, string Title, bool IsCompleted, DateTimeOffset? CompletedAtUtc);
public sealed record SubjectGoalDetails(Guid Id, Guid SubjectId, Guid TopicId, string Title, GoalKind Kind, StudyMetricDefinitionDetails? MetricDefinition, decimal? TargetValue, decimal? CurrentValue, DateOnly? TargetDate, GoalPeriod Period, DateOnly? PeriodStartDate, DateOnly? PeriodEndDate, long PriorityPosition, bool IsCompleted, DateTimeOffset? CompletedAtUtc, DateTimeOffset CreatedAtUtc, IReadOnlyCollection<SubjectSubGoalDetails> SubGoals);
