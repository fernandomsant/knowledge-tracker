namespace KnowledgeTracker.Domain.Knowledge;

public sealed record SubjectConnection
{
    public SubjectConnection(Guid subjectId, Guid connectedSubjectId)
        : this(Guid.NewGuid(), subjectId, connectedSubjectId)
    {
    }

    public SubjectConnection(Guid id, Guid subjectId, Guid connectedSubjectId)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Subject-connection identifier is required.", nameof(id));
        if (subjectId == Guid.Empty)
            throw new ArgumentException("Subject identifier is required.", nameof(subjectId));
        if (connectedSubjectId == Guid.Empty)
            throw new ArgumentException("Connected-subject identifier is required.", nameof(connectedSubjectId));
        if (subjectId == connectedSubjectId)
            throw new ArgumentException("A subject cannot be linked to itself.", nameof(connectedSubjectId));
        Id = id;
        if (subjectId.CompareTo(connectedSubjectId) < 0)
        {
            SubjectId = subjectId;
            ConnectedSubjectId = connectedSubjectId;
        }
        else
        {
            SubjectId = connectedSubjectId;
            ConnectedSubjectId = subjectId;
        }
    }

    public Guid Id { get; }
    public Guid SubjectId { get; }
    public Guid ConnectedSubjectId { get; }
}
