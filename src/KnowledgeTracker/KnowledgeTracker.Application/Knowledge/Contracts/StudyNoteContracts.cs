namespace KnowledgeTracker.Application.Knowledge;

public sealed record CreateStudyNoteRequest(
    string Title,
    string Content,
    TimeSpan StudyDuration,
    DateTimeOffset StudyStartedAtUtc
);

public sealed record UpdateStudyNoteRequest(string Title, string Content, TimeSpan StudyDuration);

public sealed record StudyNoteDetails(
    Guid Id,
    Guid SubjectId,
    string Title,
    string Content,
    TimeSpan StudyDuration,
    DateTimeOffset StudyStartedAtUtc
);
