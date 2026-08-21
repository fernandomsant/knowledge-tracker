using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

internal static class KnowledgeContractMapper
{
    public static SubjectSummary ToSummary(Subject subject, SubjectLayoutPosition? layoutPosition = null) =>
        new(subject.Id, subject.Name, subject.Description, subject.ParentSubjectId, layoutPosition is null ? null : ToDetails(layoutPosition));

    public static SubjectLayoutPositionDetails ToDetails(SubjectLayoutPosition layoutPosition) =>
        new(layoutPosition.SubjectId, layoutPosition.NormalizedX, layoutPosition.NormalizedY);

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
            )).ToArray(),
            studyNote.Version,
            new NoteClassificationDetails(
                studyNote.Classification.Status,
                studyNote.Classification.Model,
                studyNote.Classification.ModelVersion,
                studyNote.Classification.FailureReason,
                studyNote.Classification.Scores.Select(score => new NoteClassificationScoreDetails(
                    score.SubjectId, score.SubjectName, score.Score
                )).ToArray()
            )
        );

    public static SubjectConnectionDetails ToDetails(SubjectConnection connection) =>
        new(connection.Id, connection.SubjectId, connection.ConnectedSubjectId);
}
