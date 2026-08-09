using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

internal static class KnowledgeContractMapper
{
    public static SubjectSummary ToSummary(Subject subject) =>
        new(subject.Id, subject.Name, subject.Description);

    public static StudyNoteDetails ToDetails(StudyNote studyNote) =>
        new(
            studyNote.Id,
            studyNote.SubjectId,
            studyNote.Title,
            studyNote.Content,
            studyNote.StudyDuration,
            studyNote.StudyStartedAtUtc
        );

    public static SubjectConnectionDetails ToDetails(SubjectConnection connection) =>
        new(connection.Id, connection.SubjectId, connection.ConnectedSubjectId);
}
