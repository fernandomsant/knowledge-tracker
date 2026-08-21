using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using KnowledgeTracker.Web.Knowledge.Serialization;

namespace KnowledgeTracker.Web.Knowledge.Contracts;

public sealed record CreateSubjectRequest
{
    [Required]
    [StringLength(256)]
    public required string Name { get; init; }

    public string? Description { get; init; }
    public Guid? ParentSubjectId { get; init; }
}

public sealed record UpdateSubjectRequest
{
    [Required]
    [StringLength(256)]
    public required string Name { get; init; }

    public string? Description { get; init; }
    public Guid? ParentSubjectId { get; init; }
}

public sealed record SaveSubjectLayoutRequest
{
    [Required]
    public IReadOnlyCollection<SubjectLayoutPositionRequest> Positions { get; init; } = [];
}

public sealed record SubjectLayoutPositionRequest
{
    public Guid SubjectId { get; init; }

    [Range(typeof(decimal), "0", "1", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal NormalizedX { get; init; }

    [Range(typeof(decimal), "0", "1", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal NormalizedY { get; init; }
}

public sealed record CreateStudyNoteRequest
{
    public Guid TopicId { get; init; }
    [Required]
    [StringLength(512)]
    public required string Title { get; init; }

    [Required]
    public required string Content { get; init; }

    public TimeSpan StudyDuration { get; init; }

    public DateTimeOffset StudyStartedAtUtc { get; init; }

    public IReadOnlyCollection<StudyNoteMetricRequest> Metrics { get; init; } = [];
}

public sealed record CreateUnclassifiedStudyNoteRequest
{
    [Required]
    [StringLength(512)]
    public required string Title { get; init; }

    [Required]
    public required string Content { get; init; }

    public TimeSpan StudyDuration { get; init; }

    public DateTimeOffset StudyStartedAtUtc { get; init; }

    public IReadOnlyCollection<StudyNoteMetricRequest> Metrics { get; init; } = [];
}

public sealed record UpdateStudyNoteRequest
{
    public Guid? TopicId { get; init; }
    [Required]
    [StringLength(512)]
    public required string Title { get; init; }

    [Required]
    public required string Content { get; init; }

    public TimeSpan StudyDuration { get; init; }

    public DateTimeOffset StudyStartedAtUtc { get; init; }

    public IReadOnlyCollection<StudyNoteMetricRequest> Metrics { get; init; } = [];
}

public sealed record StudyNoteMetricRequest
{
    public Guid DefinitionId { get; init; }

    [Range(
        typeof(decimal),
        "0",
        "9999999999999999.99",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    public decimal Value { get; init; }
}

public sealed record CreateStudyMetricDefinitionRequest
{
    [Required]
    [StringLength(256)]
    public required string Name { get; init; }

    public KnowledgeTracker.Domain.Knowledge.MetricNumberKind NumberKind { get; init; }
}

public sealed record CreateSubjectConnectionRequest
{
    public Guid SubjectId { get; init; }
    public Guid ConnectedSubjectId { get; init; }
}

public sealed record CreateSubjectGoalRequest
{
    public Guid TopicId { get; init; }
    [Required]
    [StringLength(256)]
    public required string Title { get; init; }
    public KnowledgeTracker.Domain.Knowledge.GoalKind Kind { get; init; }
    public Guid? MetricDefinitionId { get; init; }
    [Range(
        typeof(decimal),
        "0.01",
        "9999999999999999.99",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    [JsonConverter(typeof(FlexibleNullableDecimalConverter))]
    public decimal? TargetValue { get; init; }
    public DateOnly? TargetDate { get; init; }
    public KnowledgeTracker.Domain.Knowledge.GoalPeriod Period { get; init; }
    public DateOnly? PeriodStartDate { get; init; }
    public DateOnly? PeriodEndDate { get; init; }
    public IReadOnlyCollection<string> SubGoals { get; init; } = [];
}

public sealed record UpdateSubjectGoalRequest
{
    public Guid TopicId { get; init; }
    [Required]
    [StringLength(256)]
    public required string Title { get; init; }
    public KnowledgeTracker.Domain.Knowledge.GoalKind Kind { get; init; }
    public Guid? MetricDefinitionId { get; init; }
    [Range(
        typeof(decimal),
        "0.01",
        "9999999999999999.99",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    [JsonConverter(typeof(FlexibleNullableDecimalConverter))]
    public decimal? TargetValue { get; init; }
    public DateOnly? TargetDate { get; init; }
    public KnowledgeTracker.Domain.Knowledge.GoalPeriod Period { get; init; }
    public DateOnly? PeriodStartDate { get; init; }
    public DateOnly? PeriodEndDate { get; init; }
    public IReadOnlyCollection<string> SubGoals { get; init; } = [];
}

public sealed record SetSubGoalCompletionRequest(bool IsCompleted);
public sealed record SwapSubjectGoalPriorityRequest(Guid SwapWithId);
public sealed record CreateTopicRequest { [Required, StringLength(256)] public required string Name { get; init; } }
