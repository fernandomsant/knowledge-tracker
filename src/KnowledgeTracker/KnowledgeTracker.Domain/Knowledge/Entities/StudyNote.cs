namespace KnowledgeTracker.Domain.Knowledge;

public sealed class StudyNote
{
    public StudyNote(
        Guid id,
        Guid subjectId,
        string title,
        string content,
        TimeSpan studyDuration,
        DateTimeOffset studyStartedAtUtc
    )
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Study-note identifier is required.", nameof(id));
        if (subjectId == Guid.Empty)
            throw new ArgumentException("Subject identifier is required.", nameof(subjectId));

        Id = id;
        SubjectId = subjectId;
        Update(title, content, studyDuration);
        StudyStartedAtUtc = studyStartedAtUtc;
    }

    public Guid Id { get; }
    public Guid SubjectId { get; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public TimeSpan StudyDuration { get; private set; }
    public DateTimeOffset StudyStartedAtUtc { get; }

    public void Update(string title, string content, TimeSpan studyDuration)
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
    }
}