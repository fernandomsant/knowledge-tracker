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
