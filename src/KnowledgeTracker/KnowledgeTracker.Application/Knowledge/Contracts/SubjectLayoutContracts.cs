namespace KnowledgeTracker.Application.Knowledge;

public sealed record SaveSubjectLayoutRequest(IReadOnlyCollection<SubjectLayoutPositionRequest> Positions);

public sealed record SubjectLayoutPositionRequest(Guid SubjectId, decimal NormalizedX, decimal NormalizedY);

public sealed record SubjectLayoutPositionDetails(Guid SubjectId, decimal NormalizedX, decimal NormalizedY);
