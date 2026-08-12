using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed record CreateSubjectGoalRequest(string Title, GoalKind Kind, Guid? MetricDefinitionId, decimal? TargetValue, DateOnly? TargetDate);

public sealed record SubjectGoalDetails(Guid Id, Guid SubjectId, string Title, GoalKind Kind, StudyMetricDefinitionDetails? MetricDefinition, decimal? TargetValue, decimal? CurrentValue, DateOnly? TargetDate, DateTimeOffset CreatedAtUtc);
