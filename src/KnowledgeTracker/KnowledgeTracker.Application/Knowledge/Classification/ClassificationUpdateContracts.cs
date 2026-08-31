namespace KnowledgeTracker.Application.Knowledge;

public sealed record ClassificationUpdateCheckpoint(DateTimeOffset CompletedAtUtc, Guid JobId);

public sealed record ClassificationUpdate(
    Guid JobId,
    Guid NoteId,
    DateTimeOffset CompletedAtUtc
);

public sealed record ClassificationUpdateDetails(
    ClassificationUpdateCheckpoint Checkpoint,
    StudyNoteDetails? Note
);
