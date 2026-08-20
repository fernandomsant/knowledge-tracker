namespace KnowledgeTracker.Domain.Knowledge;

public sealed class StudyNote
{
    private readonly List<StudyNoteMetric> metrics = [];

    public StudyNote(
        Guid id,
        Guid subjectId,
        Guid topicId,
        string title,
        string content,
        TimeSpan studyDuration,
        DateTimeOffset studyStartedAtUtc,
        IEnumerable<StudyNoteMetric>? metrics = null,
        long version = 1,
        NoteClassificationState? classification = null
    )
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Study-note identifier is required.", nameof(id));
        if (subjectId == Guid.Empty)
            throw new ArgumentException("Subject identifier is required.", nameof(subjectId));
        if (topicId == Guid.Empty)
            throw new ArgumentException("Topic identifier is required.", nameof(topicId));
        if (version <= 0)
            throw new ArgumentOutOfRangeException(nameof(version), "Study-note version must be positive.");

        Id = id;
        SubjectId = subjectId;
        TopicId = topicId;
        Version = version;
        Classification = classification ?? NoteClassificationState.Pending;
        Update(title, content, studyDuration, metrics);
        StudyStartedAtUtc = studyStartedAtUtc;
    }

    public Guid Id { get; }
    public Guid SubjectId { get; }
    public Guid TopicId { get; }
    public long Version { get; }
    public NoteClassificationState Classification { get; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public TimeSpan StudyDuration { get; private set; }
    public DateTimeOffset StudyStartedAtUtc { get; private set; }
    public IReadOnlyCollection<StudyNoteMetric> Metrics => metrics.AsReadOnly();

    public StudyNote(Guid id, Guid subjectId, string title, string content, TimeSpan studyDuration, DateTimeOffset studyStartedAtUtc, IEnumerable<StudyNoteMetric>? metrics = null)
        : this(id, subjectId, subjectId, title, content, studyDuration, studyStartedAtUtc, metrics)
    {
    }

    public void Update(
        string title,
        string content,
        TimeSpan studyDuration,
        IEnumerable<StudyNoteMetric>? updatedMetrics = null
    )
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Study-note title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Study-note content is required.", nameof(content));
        if (studyDuration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(studyDuration));

        Title = title.Trim();
        Content = content.Trim();
        StudyDuration = studyDuration;
        ReplaceMetrics(updatedMetrics ?? []);
    }

    public void SetStudyStartedAtUtc(DateTimeOffset studyStartedAtUtc)
    {
        StudyStartedAtUtc = studyStartedAtUtc;
    }

    private void ReplaceMetrics(IEnumerable<StudyNoteMetric> updatedMetrics)
    {
        ArgumentNullException.ThrowIfNull(updatedMetrics);
        var metricList = updatedMetrics.ToArray();
        if (metricList.GroupBy(metric => metric.Definition.Id).Any(group => group.Count() > 1))
            throw new ArgumentException("A study note cannot have duplicate metric names.", nameof(updatedMetrics));

        metrics.Clear();
        metrics.AddRange(metricList);
    }
}
