namespace KnowledgeTracker.Web.Knowledge.Contracts;

public sealed record SubjectSummaryResponse(Guid Id, string Name, string? Description, Guid? ParentSubjectId);

public sealed record SubjectDetailsResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentSubjectId,
    IReadOnlyCollection<StudyNoteResponse> StudyNotes
);

public sealed record StudyNoteResponse(
    Guid Id,
    Guid SubjectId,
    string Title,
    string Content,
    TimeSpan StudyDuration,
    DateTimeOffset StudyStartedAtUtc,
    IReadOnlyCollection<StudyNoteMetricResponse> Metrics
);

public sealed record StudyMetricDefinitionResponse(Guid Id, string Name, KnowledgeTracker.Domain.Knowledge.MetricNumberKind NumberKind);

public sealed record StudyNoteMetricResponse(StudyMetricDefinitionResponse Definition, decimal Value);

public sealed record SubjectConnectionResponse(Guid Id, Guid SubjectId, Guid ConnectedSubjectId);

public sealed record SubjectSubGoalResponse(Guid Id, string Title, bool IsCompleted, DateTimeOffset? CompletedAtUtc);
public sealed record SubjectGoalResponse(Guid Id, Guid SubjectId, string Title, KnowledgeTracker.Domain.Knowledge.GoalKind Kind, StudyMetricDefinitionResponse? MetricDefinition, decimal? TargetValue, decimal? CurrentValue, DateOnly? TargetDate, KnowledgeTracker.Domain.Knowledge.GoalPeriod Period, DateOnly? PeriodStartDate, DateOnly? PeriodEndDate, bool IsCompleted, DateTimeOffset? CompletedAtUtc, DateTimeOffset CreatedAtUtc, IReadOnlyCollection<SubjectSubGoalResponse> SubGoals);
