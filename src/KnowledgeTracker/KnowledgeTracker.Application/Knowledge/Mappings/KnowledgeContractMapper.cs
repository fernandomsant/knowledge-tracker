using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

internal static class KnowledgeContractMapper
{
    public static SubjectSummary ToSummary(Subject subject) =>
        new(subject.Id, subject.Name, subject.Description, subject.ParentSubjectId);

    public static StudyNoteDetails ToDetails(StudyNote studyNote) =>
        new(
            studyNote.Id,
            studyNote.SubjectId,
            studyNote.TopicId,
            studyNote.Title,
            studyNote.Content,
            studyNote.StudyDuration,
            studyNote.StudyStartedAtUtc,
            studyNote.Metrics.Select(metric => new StudyNoteMetricDetails(
                new StudyMetricDefinitionDetails(metric.Definition.Id, metric.Definition.Name, metric.Definition.NumberKind),
                metric.Value
            )).ToArray()
        );

    public static SubjectConnectionDetails ToDetails(SubjectConnection connection) =>
        new(connection.Id, connection.SubjectId, connection.ConnectedSubjectId);
}
