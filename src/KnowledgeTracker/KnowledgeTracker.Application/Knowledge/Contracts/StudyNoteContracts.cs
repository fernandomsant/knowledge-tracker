namespace KnowledgeTracker.Application.Knowledge;

public sealed record CreateStudyNoteRequest(
    Guid TopicId,
    string Title,
    string Content,
    TimeSpan StudyDuration,
    DateTimeOffset StudyStartedAtUtc,
    IReadOnlyCollection<StudyNoteMetricRequest> Metrics
);

public sealed record UpdateStudyNoteRequest(
    Guid TopicId,
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
    Guid TopicId,
    string Title,
    string Content,
    TimeSpan StudyDuration,
    DateTimeOffset StudyStartedAtUtc,
    IReadOnlyCollection<StudyNoteMetricDetails> Metrics
);

public sealed record StudyNoteMetricDetails(StudyMetricDefinitionDetails Definition, decimal Value);
