namespace KnowledgeTracker.Web.Knowledge.Contracts;

/// <summary>Represents one expected goal occurrence and its authoritative completion timestamp.</summary>
public sealed record GoalActivityResponse(
    Guid GoalId,
    Guid SubjectId,
    Guid TopicId,
    string GoalTitle,
    DateOnly OccurrenceStartDate,
    DateOnly OccurrenceEndDate,
    DateTimeOffset? CompletedAtUtc);
