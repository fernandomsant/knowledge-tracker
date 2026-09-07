using KnowledgeTracker.Application.Knowledge;

namespace KnowledgeTracker.Mcp.ApplicationApi;

public interface IApplicationApiClient
{
    Task<IReadOnlyCollection<SubjectSummary>> ListSubjectsAsync(CancellationToken ct);
    Task<SubjectDetails?> GetSubjectAsync(Guid id, CancellationToken ct);
    Task<SubjectSummary> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<TopicDetails>> ListTopicsAsync(CancellationToken ct);
    Task<TopicDetails> CreateTopicAsync(Guid subjectId, string name, CancellationToken ct);
    Task<IReadOnlyCollection<StudyNoteDetails>> ListNotesAsync(Guid subjectId, bool includeDescendants, CancellationToken ct);
    Task<StudyNoteDetails?> CreateNoteAsync(Guid subjectId, CreateStudyNoteRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<SubjectGoalDetails>> ListGoalsAsync(Guid subjectId, CancellationToken ct);
    Task<SubjectGoalDetails?> CreateGoalAsync(Guid subjectId, CreateSubjectGoalRequest request, CancellationToken ct);
    Task<bool> CompleteGoalAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<GoalActivityDetails>> ListGoalActivityAsync(DateOnly from, DateOnly to, CancellationToken ct);
}
