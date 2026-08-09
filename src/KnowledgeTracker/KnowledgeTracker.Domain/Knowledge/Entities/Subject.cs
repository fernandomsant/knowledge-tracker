namespace KnowledgeTracker.Domain.Knowledge;

public sealed class Subject
{
    private readonly List<StudyNote> studyNotes = [];

    public Subject(string name, string? description = null)
        : this(Guid.NewGuid(), name, description)
    {
    }

    public Subject(Guid id, string name, string? description = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Subject identifier is required.", nameof(id));

        Id = id;
        Rename(name);
        UpdateDescription(description);
    }

    public Guid Id { get; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
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

    public StudyNote AddStudyNote(
        string title,
        string content,
        TimeSpan studyDuration,
        DateTimeOffset studyStartedAtUtc
    )
    {
        var note = new StudyNote(
            Guid.NewGuid(),
            Id,
            title,
            content,
            studyDuration,
            studyStartedAtUtc
        );
        studyNotes.Add(note);
        return note;
    }
}