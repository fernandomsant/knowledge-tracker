namespace KnowledgeTracker.Application.Knowledge;

public sealed record ClassificationJob(
    Guid Id,
    Guid NoteId,
    long NoteVersion,
    long TaxonomyVersion,
    int Attempts,
    string WorkerId
);

public sealed record ClassificationNode(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentSubjectId
);

public sealed record ClassificationWorkItem(
    ClassificationJob Job,
    string Text,
    IReadOnlyCollection<ClassificationNode> Nodes
);

public sealed record ClassifierScore(Guid NodeId, double Score);

public sealed record ClassifierResult(
    string Model,
    string ModelVersion,
    IReadOnlyCollection<ClassifierScore> Classifications
);

public enum ClassificationCompletionOutcome
{
    Completed,
    Superseded
}
