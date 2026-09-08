namespace KnowledgeTracker.Domain.Knowledge;

public sealed class Subject
{
    private readonly List<StudyNote> studyNotes = [];

    public Subject(string name, string? description = null, Guid? parentSubjectId = null)
        : this(Guid.NewGuid(), null, null, name, description, parentSubjectId)
    {
    }

    public Subject(Guid id, string name, string? description = null, Guid? parentSubjectId = null)
        : this(id, null, null, name, description, parentSubjectId)
    {
    }

    public Subject(Guid userId, Guid workspaceId, string name, string? description = null, Guid? parentSubjectId = null)
        : this(Guid.NewGuid(), userId, workspaceId, name, description, parentSubjectId)
    {
    }

    public Subject(Guid id, Guid userId, Guid workspaceId, string name, string? description = null, Guid? parentSubjectId = null)
        : this(id, (Guid?)userId, (Guid?)workspaceId, name, description, parentSubjectId)
    {
    }

    private Subject(Guid id, Guid? userId, Guid? workspaceId, string name, string? description, Guid? parentSubjectId)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Subject identifier is required.", nameof(id));
        if (userId is { } requiredUserId && requiredUserId == Guid.Empty)
            throw new ArgumentException("Subject user identifier is required.", nameof(userId));
        if (workspaceId is { } requiredWorkspaceId && requiredWorkspaceId == Guid.Empty)
            throw new ArgumentException("Subject workspace identifier is required.", nameof(workspaceId));

        Id = id;
        UserId = userId ?? Guid.Empty;
        WorkspaceId = workspaceId ?? Guid.Empty;
        Rename(name);
        UpdateDescription(description);
        SetParent(parentSubjectId);
    }

    public Guid Id { get; }
    public Guid UserId { get; }
    public Guid WorkspaceId { get; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? ParentSubjectId { get; private set; }
    public IReadOnlyCollection<StudyNote> StudyNotes => studyNotes.AsReadOnly();

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Subject name is required.", nameof(name));

        Name = name.Trim();
    }

    public void UpdateDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void SetParent(Guid? parentSubjectId)
    {
        if (parentSubjectId == Id)
            throw new ArgumentException("A subject cannot be its own parent.", nameof(parentSubjectId));

        ParentSubjectId = parentSubjectId;
    }

    public StudyNote AddStudyNote(
        Guid topicId,
        string title,
        string content,
        TimeSpan studyDuration,
        DateTimeOffset studyStartedAtUtc,
        IEnumerable<StudyNoteMetric>? metrics = null
    )
    {
        var note = new StudyNote(
            Guid.NewGuid(),
            Id,
            topicId,
            title,
            content,
            studyDuration,
            studyStartedAtUtc,
            metrics
        );
        studyNotes.Add(note);
        return note;
    }

    public StudyNote AddStudyNote(string title, string content, TimeSpan studyDuration, DateTimeOffset studyStartedAtUtc, IEnumerable<StudyNoteMetric>? metrics = null) =>
        AddStudyNote(Id, title, content, studyDuration, studyStartedAtUtc, metrics);
}
