namespace KnowledgeTracker.Domain.Knowledge;

public sealed class Subject
{
    private readonly List<StudyNote> studyNotes = [];

    public Subject(string name, string? description = null, Guid? parentSubjectId = null)
        : this(Guid.NewGuid(), name, description, parentSubjectId)
    {
    }

    public Subject(Guid id, string name, string? description = null, Guid? parentSubjectId = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Subject identifier is required.", nameof(id));

        Id = id;
        Rename(name);
        UpdateDescription(description);
        SetParent(parentSubjectId);
    }

    public Guid Id { get; }
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
            title,
            content,
            studyDuration,
            studyStartedAtUtc,
            metrics
        );
        studyNotes.Add(note);
        return note;
    }
}
