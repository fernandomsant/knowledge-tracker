namespace KnowledgeTracker.Domain.Knowledge;

public enum NoteClassificationStatus : byte
{
    Pending = 0,
    Processing = 1,
    RetryScheduled = 2,
    Completed = 3,
    Failed = 4,
    Superseded = 5
}

public enum NoteNodeRelationSource : byte
{
    Manual = 0,
    Classifier = 1,
    Inherited = 2
}

public sealed record NoteClassificationScore
{
    public NoteClassificationScore(Guid subjectId, string subjectName, double score)
    {
        if (subjectId == Guid.Empty)
            throw new ArgumentException("Subject identifier is required.", nameof(subjectId));
        if (string.IsNullOrWhiteSpace(subjectName))
            throw new ArgumentException("Subject name is required.", nameof(subjectName));
        if (!double.IsFinite(score) || score < 0 || score > 1)
            throw new ArgumentOutOfRangeException(nameof(score), "Classification score must be between zero and one.");

        SubjectId = subjectId;
        SubjectName = subjectName.Trim();
        Score = score;
    }

    public Guid SubjectId { get; }
    public string SubjectName { get; }
    public double Score { get; }
}

public sealed class NoteClassificationState
{
    public NoteClassificationState(
        NoteClassificationStatus status,
        string? model = null,
        string? modelVersion = null,
        string? failureReason = null,
        IEnumerable<NoteClassificationScore>? scores = null
    )
    {
        Status = status;
        Model = string.IsNullOrWhiteSpace(model) ? null : model.Trim();
        ModelVersion = string.IsNullOrWhiteSpace(modelVersion) ? null : modelVersion.Trim();
        FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason.Trim();
        Scores = (scores ?? []).OrderByDescending(item => item.Score).ToArray();
    }

    public NoteClassificationStatus Status { get; }
    public string? Model { get; }
    public string? ModelVersion { get; }
    public string? FailureReason { get; }
    public IReadOnlyCollection<NoteClassificationScore> Scores { get; }

    public static NoteClassificationState Pending { get; } = new(NoteClassificationStatus.Pending);
}
