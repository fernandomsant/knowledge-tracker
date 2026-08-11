namespace KnowledgeTracker.Application.Knowledge;

public sealed record CreateStudyNoteRequest(
    string Title,
    string Content,
    TimeSpan StudyDuration,
    DateTimeOffset StudyStartedAtUtc,
    IReadOnlyCollection<StudyNoteMetricRequest> Metrics
);

public sealed record UpdateStudyNoteRequest(
    string Title,
    string Content,
    TimeSpan StudyDuration,
    DateTimeOffset StudyStartedAtUtc,
    IReadOnlyCollection<StudyNoteMetricRequest> Metrics
);

public sealed record StudyNoteMetricRequest(Guid DefinitionId, decimal Value);

public sealed record StudyNoteDetails(
    Guid Id,
    Guid SubjectId,
    string Title,
    string Content,
    TimeSpan StudyDuration,
    DateTimeOffset StudyStartedAtUtc,
    IReadOnlyCollection<StudyNoteMetricDetails> Metrics
);

public sealed record StudyNoteMetricDetails(StudyMetricDefinitionDetails Definition, decimal Value);
