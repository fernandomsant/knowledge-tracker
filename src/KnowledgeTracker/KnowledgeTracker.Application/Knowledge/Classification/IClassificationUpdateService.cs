namespace KnowledgeTracker.Application.Knowledge;

public interface IClassificationUpdateService
{
    Task<IReadOnlyCollection<ClassificationUpdateDetails>> ListAfterAsync(
        ClassificationUpdateCheckpoint checkpoint,
        int take,
        CancellationToken ct
    );
}
