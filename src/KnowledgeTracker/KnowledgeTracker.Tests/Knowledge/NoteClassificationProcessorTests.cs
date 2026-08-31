using KnowledgeTracker.Application.Knowledge;
using Xunit;

namespace KnowledgeTracker.Tests.Knowledge;

public sealed class NoteClassificationProcessorTests
{
    [Fact]
    public async Task ProcessNextAsync_returns_false_when_queue_is_empty()
    {
        var repository = new FakeClassificationJobRepository();
        var classifier = new FakeNoteClassifier();
        var processor = new NoteClassificationProcessor(repository, classifier);

        var processed = await processor.ProcessNextAsync("worker-1", CancellationToken.None);

        Assert.False(processed);
        Assert.Equal(0, classifier.Calls);
    }

    [Fact]
    public async Task ProcessNextAsync_persists_a_valid_score_for_every_taxonomy_node()
    {
        var node = new ClassificationNode(Guid.NewGuid(), "Linux", "Operating systems", null);
        var repository = FakeClassificationJobRepository.WithWorkItem(node);
        var classifier = new FakeNoteClassifier
        {
            Result = new ClassifierResult(
                "gliclass",
                "1.0",
                [new ClassifierScore(node.Id, 0.91)]
            )
        };
        var processor = new NoteClassificationProcessor(repository, classifier);

        var processed = await processor.ProcessNextAsync("worker-1", CancellationToken.None);

        Assert.True(processed);
        Assert.NotNull(repository.CompletedResult);
        Assert.Equal(0.91, repository.CompletedResult.Classifications.Single().Score, 3);
        Assert.Null(repository.FailureError);
    }

    [Fact]
    public async Task ProcessNextAsync_schedules_a_retry_when_a_taxonomy_score_is_missing()
    {
        var node = new ClassificationNode(Guid.NewGuid(), "Linux", null, null);
        var repository = FakeClassificationJobRepository.WithWorkItem(node);
        var classifier = new FakeNoteClassifier
        {
            Result = new ClassifierResult("gliclass", "1.0", [])
        };
        var processor = new NoteClassificationProcessor(repository, classifier);

        var processed = await processor.ProcessNextAsync("worker-1", CancellationToken.None);

        Assert.True(processed);
        Assert.Null(repository.CompletedResult);
        Assert.Contains("one score for every supplied node", repository.FailureError);
        Assert.NotNull(repository.RetryAtUtc);
    }

    private sealed class FakeNoteClassifier : INoteClassifier
    {
        public int Calls { get; private set; }
        public ClassifierResult Result { get; init; } = new("unused", "unused", []);

        public Task<ClassifierResult> ClassifyAsync(
            string text,
            IReadOnlyCollection<ClassificationNode> nodes,
            CancellationToken ct
        )
        {
            Calls++;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeClassificationJobRepository : IClassificationJobRepository
    {
        private ClassificationJob? job;
        private ClassificationWorkItem? workItem;

        public ClassifierResult? CompletedResult { get; private set; }
        public string? FailureError { get; private set; }
        public DateTimeOffset? RetryAtUtc { get; private set; }

        public static FakeClassificationJobRepository WithWorkItem(params ClassificationNode[] nodes)
        {
            var repository = new FakeClassificationJobRepository();
            repository.job = new ClassificationJob(
                Guid.NewGuid(), Guid.NewGuid(), 1, 1, 1, "worker-1"
            );
            repository.workItem = new ClassificationWorkItem(
                repository.job, "Study Linux namespaces", nodes
            );
            return repository;
        }

        public Task<ClassificationJob?> ClaimNextAsync(
            string workerId,
            TimeSpan leaseDuration,
            CancellationToken ct
        ) => Task.FromResult(job);

        public Task<ClassificationWorkItem?> LoadWorkItemAsync(
            ClassificationJob claimedJob,
            CancellationToken ct
        ) => Task.FromResult(workItem);

        public Task<ClassificationCompletionOutcome> CompleteAsync(
            ClassificationJob completedJob,
            ClassifierResult result,
            double relationThreshold,
            CancellationToken ct
        )
        {
            CompletedResult = result;
            return Task.FromResult(ClassificationCompletionOutcome.Completed);
        }

        public Task RecordFailureAsync(
            ClassificationJob failedJob,
            string error,
            DateTimeOffset? retryAtUtc,
            CancellationToken ct
        )
        {
            FailureError = error;
            RetryAtUtc = retryAtUtc;
            return Task.CompletedTask;
        }
    }
}
