namespace KnowledgeTracker.Application.Knowledge;

public sealed record CreateSubjectConnectionRequest(Guid SubjectId, Guid ConnectedSubjectId);

public sealed record SubjectConnectionDetails(
    Guid Id,
    Guid SubjectId,
    Guid ConnectedSubjectId
);
