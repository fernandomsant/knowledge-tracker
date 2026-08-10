namespace KnowledgeTracker.Domain.Knowledge;

public sealed class StudyNote
{
    private readonly List<StudyNoteMetric> metrics = [];

    public StudyNote(
        Guid id,
        Guid subjectId,
        string title,
        string content,
        TimeSpan studyDuration,
        DateTimeOffset studyStartedAtUtc,
        IEnumerable<StudyNoteMetric>? metrics = null
    )
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Study-note identifier is required.", nameof(id));
        if (subjectId == Guid.Empty)
            throw new ArgumentException("Subject identifier is required.", nameof(subjectId));

        Id = id;
        SubjectId = subjectId;
        Update(title, content, studyDuration, metrics);
        StudyStartedAtUtc = studyStartedAtUtc;
    }

    public Guid Id { get; }
    public Guid SubjectId { get; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public TimeSpan StudyDuration { get; private set; }
    public DateTimeOffset StudyStartedAtUtc { get; }
    public IReadOnlyCollection<StudyNoteMetric> Metrics => metrics.AsReadOnly();

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
