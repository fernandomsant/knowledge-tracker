namespace KnowledgeTracker.Application.Knowledge;

public sealed class NoteClassificationProcessor(
    IClassificationJobRepository jobs,
    INoteClassifier classifier
)
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    private const int MaximumAttempts = 3;
    private const double DefaultRelationThreshold = 0.5;

    public async Task<bool> ProcessNextAsync(string workerId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(workerId))
            throw new ArgumentException("Worker identifier is required.", nameof(workerId));

        var job = await jobs.ClaimNextAsync(workerId.Trim(), LeaseDuration, ct);
        if (job is null)
            return false;

        try
        {
            var workItem = await jobs.LoadWorkItemAsync(job, ct);
            if (workItem is null)
                return true;

            var result = await classifier.ClassifyAsync(workItem.Text, workItem.Nodes, ct);
            ValidateResult(result, workItem.Nodes);
            await jobs.CompleteAsync(job, result, DefaultRelationThreshold, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            DateTimeOffset? retryAtUtc = job.Attempts >= MaximumAttempts
                ? null
                : DateTimeOffset.UtcNow.Add(RetryDelay(job.Attempts));
            await jobs.RecordFailureAsync(job, exception.Message, retryAtUtc, ct);
        }

        return true;
    }

    private static void ValidateResult(
        ClassifierResult result,
        IReadOnlyCollection<ClassificationNode> nodes
    )
    {
        ArgumentNullException.ThrowIfNull(result);
        if (string.IsNullOrWhiteSpace(result.Model))
            throw new InvalidOperationException("The classifier did not identify its model.");
        if (string.IsNullOrWhiteSpace(result.ModelVersion))
            throw new InvalidOperationException("The classifier did not identify its model version.");

        var validNodeIds = nodes.Select(node => node.Id).ToHashSet();
        if (result.Classifications.Select(item => item.NodeId).Distinct().Count() != result.Classifications.Count)
            throw new InvalidOperationException("The classifier returned duplicate nodes.");
        if (result.Classifications.Count != validNodeIds.Count)
            throw new InvalidOperationException("The classifier did not return one score for every supplied node.");

        foreach (var score in result.Classifications)
        {
            if (!validNodeIds.Contains(score.NodeId))
                throw new InvalidOperationException("The classifier returned a node outside the supplied taxonomy.");
            if (!double.IsFinite(score.Score) || score.Score < 0 || score.Score > 1)
                throw new InvalidOperationException("The classifier returned an invalid score.");
        }
    }

    private static TimeSpan RetryDelay(int attempts) => attempts switch
    {
        <= 1 => TimeSpan.FromSeconds(10),
        _ => TimeSpan.FromMinutes(1)
    };
}
