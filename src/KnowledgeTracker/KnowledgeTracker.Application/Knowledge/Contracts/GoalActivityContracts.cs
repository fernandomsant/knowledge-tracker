using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed record GoalActivityDetails(
    Guid GoalId,
    Guid SubjectId,
    Guid TopicId,
    string GoalTitle,
    GoalKind GoalKind,
    GoalPeriod Period,
    DateOnly OccurrenceStartDate,
    DateOnly OccurrenceEndDate,
    DateTimeOffset? CompletedAtUtc);
