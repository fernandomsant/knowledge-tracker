using KnowledgeTracker.Application.Knowledge;

namespace KnowledgeTracker.Mcp.ApplicationApi;

public interface IApplicationApiClient
{
    Task<IReadOnlyCollection<WorkspaceDetails>> ListWorkspacesAsync(CancellationToken ct);
    Task<IReadOnlyCollection<SubjectSummary>> ListSubjectsAsync(Guid workspaceId, CancellationToken ct);
    Task<SubjectDetails?> GetSubjectAsync(Guid workspaceId, Guid id, CancellationToken ct);
    Task<SubjectSummary> CreateSubjectAsync(Guid workspaceId, CreateSubjectRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<TopicDetails>> ListTopicsAsync(Guid workspaceId, CancellationToken ct);
    Task<TopicDetails> CreateTopicAsync(Guid workspaceId, Guid subjectId, string name, CancellationToken ct);
    Task<IReadOnlyCollection<StudyNoteDetails>> ListNotesAsync(Guid workspaceId, Guid subjectId, bool includeDescendants, CancellationToken ct);
    Task<StudyNoteDetails?> CreateNoteAsync(Guid workspaceId, Guid subjectId, CreateStudyNoteRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<SubjectGoalDetails>> ListGoalsAsync(Guid workspaceId, Guid subjectId, CancellationToken ct);
    Task<SubjectGoalDetails?> CreateGoalAsync(Guid workspaceId, Guid subjectId, CreateSubjectGoalRequest request, CancellationToken ct);
    Task<bool> CompleteGoalAsync(Guid workspaceId, Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<GoalActivityDetails>> ListGoalActivityAsync(Guid workspaceId, DateOnly from, DateOnly to, CancellationToken ct);
}
