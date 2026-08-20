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
    IReadOnlyCollection<StudyNoteMetricDetails> Metrics,
    long Version,
    NoteClassificationDetails Classification
);

public sealed record StudyNoteMetricDetails(StudyMetricDefinitionDetails Definition, decimal Value);

public sealed record NoteClassificationDetails(
    KnowledgeTracker.Domain.Knowledge.NoteClassificationStatus Status,
    string? Model,
    string? ModelVersion,
    string? FailureReason,
    IReadOnlyCollection<NoteClassificationScoreDetails> Scores
);

public sealed record NoteClassificationScoreDetails(Guid SubjectId, string SubjectName, double Score);
