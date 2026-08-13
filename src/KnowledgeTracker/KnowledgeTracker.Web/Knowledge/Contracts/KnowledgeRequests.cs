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

public sealed record CreateStudyNoteRequest
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
}
