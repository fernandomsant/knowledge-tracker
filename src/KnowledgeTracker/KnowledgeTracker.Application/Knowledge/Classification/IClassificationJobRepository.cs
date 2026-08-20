namespace KnowledgeTracker.Application.Knowledge;

public interface IClassificationJobRepository
{
    Task<ClassificationJob?> ClaimNextAsync(string workerId, TimeSpan leaseDuration, CancellationToken ct);
    Task<ClassificationWorkItem?> LoadWorkItemAsync(ClassificationJob job, CancellationToken ct);
    Task<ClassificationCompletionOutcome> CompleteAsync(
        ClassificationJob job,
        ClassifierResult result,
        double relationThreshold,
        CancellationToken ct
    );
    Task RecordFailureAsync(
        ClassificationJob job,
        string error,
        DateTimeOffset? retryAtUtc,
        CancellationToken ct
    );
}
