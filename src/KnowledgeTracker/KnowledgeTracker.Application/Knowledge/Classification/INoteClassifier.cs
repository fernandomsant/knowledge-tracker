namespace KnowledgeTracker.Application.Knowledge;

public interface INoteClassifier
{
    Task<ClassifierResult> ClassifyAsync(
        string text,
        IReadOnlyCollection<ClassificationNode> nodes,
        CancellationToken ct
    );
}
