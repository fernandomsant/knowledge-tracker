namespace KnowledgeTracker.Application.Knowledge;

public interface IClassificationUpdateRepository
{
    Task<IReadOnlyCollection<ClassificationUpdate>> ListAfterAsync(
        ClassificationUpdateCheckpoint checkpoint,
        int take,
        CancellationToken ct
    );
}
