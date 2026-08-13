using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Web.Knowledge.Contracts;

namespace KnowledgeTracker.Web.Knowledge.Mappings;

internal static class KnowledgeResponseMapper
{
    public static SubjectSummaryResponse ToResponse(SubjectSummary subject) =>
        new(subject.Id, subject.Name, subject.Description, subject.ParentSubjectId);

    public static SubjectDetailsResponse ToResponse(SubjectDetails subject) =>
        new(
            subject.Id,
            subject.Name,
            subject.Description,
            subject.ParentSubjectId,
            subject.StudyNotes.Select(ToResponse).ToArray()
        );

    public static StudyNoteResponse ToResponse(StudyNoteDetails studyNote) =>
        new(
            studyNote.Id,
            studyNote.SubjectId,
            studyNote.Title,
            studyNote.Content,
            studyNote.StudyDuration,
            studyNote.StudyStartedAtUtc,
            studyNote.Metrics.Select(metric => new StudyNoteMetricResponse(ToResponse(metric.Definition), metric.Value)).ToArray()
        );

    public static StudyMetricDefinitionResponse ToResponse(StudyMetricDefinitionDetails definition) =>
        new(definition.Id, definition.Name, definition.NumberKind);

    public static SubjectConnectionResponse ToResponse(SubjectConnectionDetails connection) =>
        new(connection.Id, connection.SubjectId, connection.ConnectedSubjectId);

    public static SubjectGoalResponse ToResponse(SubjectGoalDetails goal) =>
        new(goal.Id, goal.SubjectId, goal.Title, goal.Kind, goal.MetricDefinition is null ? null : ToResponse(goal.MetricDefinition), goal.TargetValue, goal.CurrentValue, goal.TargetDate, goal.Period, goal.PeriodStartDate, goal.PeriodEndDate, goal.IsCompleted, goal.CompletedAtUtc, goal.CreatedAtUtc, goal.SubGoals.Select(item => new SubjectSubGoalResponse(item.Id, item.Title, item.IsCompleted, item.CompletedAtUtc)).ToArray());
}
