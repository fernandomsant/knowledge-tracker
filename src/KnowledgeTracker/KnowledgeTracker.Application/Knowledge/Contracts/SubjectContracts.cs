namespace KnowledgeTracker.Application.Knowledge;

public sealed record CreateSubjectRequest(string Name, string? Description);

public sealed record UpdateSubjectRequest(string Name, string? Description);

public sealed record SubjectSummary(Guid Id, string Name, string? Description);

public sealed record SubjectDetails(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyCollection<StudyNoteDetails> StudyNotes
);
