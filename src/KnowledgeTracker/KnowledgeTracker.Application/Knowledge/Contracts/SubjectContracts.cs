namespace KnowledgeTracker.Application.Knowledge;

public sealed record CreateSubjectRequest(string Name, string? Description, Guid? ParentSubjectId);

public sealed record UpdateSubjectRequest(string Name, string? Description, Guid? ParentSubjectId);

public sealed record SubjectSummary(Guid Id, string Name, string? Description, Guid? ParentSubjectId);

public sealed record SubjectDetails(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentSubjectId,
    IReadOnlyCollection<StudyNoteDetails> StudyNotes
);
