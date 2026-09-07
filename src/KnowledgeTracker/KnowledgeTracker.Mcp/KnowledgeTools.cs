using System.ComponentModel;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Mcp.ApplicationApi;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace KnowledgeTracker.Mcp;

[McpServerToolType]
public sealed class KnowledgeTools(
    IApplicationApiClient application)
{
    [McpServerTool(Name = "list_subjects"), Description("Lists all subjects in the knowledge tracker. Requires subjects:read.")]
    public Task<IReadOnlyCollection<SubjectSummary>> ListSubjectsAsync(CancellationToken cancellationToken)
    {
        return McpErrorMapper.ExecuteAsync(() => application.ListSubjectsAsync(cancellationToken));
    }

    [McpServerTool(Name = "get_subject"), Description("Gets one subject, including its notes and layout position. Requires subjects:read.")]
    public async Task<SubjectDetails> GetSubjectAsync(
        [Description("The subject identifier.")] Guid subjectId,
        CancellationToken cancellationToken)
    {
        return await McpErrorMapper.ExecuteAsync(() => application.GetSubjectAsync(subjectId, cancellationToken))
            ?? throw new KeyNotFoundException($"Subject '{subjectId}' was not found.");
    }

    [McpServerTool(Name = "create_subject"), Description("Creates a subject and optionally places it below a parent subject. Requires subjects:write.")]
    public Task<SubjectSummary> CreateSubjectAsync(
        [Description("The subject name.")] string name,
        [Description("An optional subject description.")] string? description,
        [Description("The optional parent subject identifier.")] Guid? parentSubjectId,
        CancellationToken cancellationToken)
    {
        return McpErrorMapper.ExecuteAsync(() => application.CreateSubjectAsync(
            new CreateSubjectRequest(name, description, parentSubjectId), cancellationToken));
    }

    [McpServerTool(Name = "list_topics"), Description("Lists topics that can be used by notes and goals. Requires topics:read.")]
    public Task<IReadOnlyCollection<TopicDetails>> ListTopicsAsync(CancellationToken cancellationToken)
    {
        return McpErrorMapper.ExecuteAsync(() => application.ListTopicsAsync(cancellationToken));
    }

    [McpServerTool(Name = "create_topic"), Description("Creates a topic under a subject. Requires topics:write.")]
    public Task<TopicDetails> CreateTopicAsync(
        [Description("The owning subject identifier.")] Guid subjectId,
        [Description("The topic name.")] string name,
        CancellationToken cancellationToken)
    {
        return McpErrorMapper.ExecuteAsync(() => application.CreateTopicAsync(subjectId, name, cancellationToken));
    }

    [McpServerTool(Name = "list_notes"), Description("Lists notes directly owned by a subject or, optionally, its descendants. Requires notes:read.")]
    public Task<IReadOnlyCollection<StudyNoteDetails>> ListNotesAsync(
        [Description("The subject identifier.")] Guid subjectId,
        [Description("When true, include notes owned by descendant subjects.")] bool includeDescendants,
        CancellationToken cancellationToken)
    {
        return McpErrorMapper.ExecuteAsync(() => application.ListNotesAsync(
            subjectId, includeDescendants, cancellationToken));
    }

    [McpServerTool(Name = "create_note"), Description("Creates a study note by invoking the application note use case. Requires notes:write.")]
    public async Task<StudyNoteDetails> CreateNoteAsync(
        [Description("The owning leaf subject identifier.")] Guid subjectId,
        CreateStudyNoteRequest request,
        CancellationToken cancellationToken)
    {
        return await McpErrorMapper.ExecuteAsync(() => application.CreateNoteAsync(subjectId, request, cancellationToken))
            ?? throw new KeyNotFoundException($"Subject '{subjectId}' was not found.");
    }

    [McpServerTool(Name = "list_goals"), Description("Lists goals belonging to a subject. Requires goals:read.")]
    public Task<IReadOnlyCollection<SubjectGoalDetails>> ListGoalsAsync(
        [Description("The subject identifier.")] Guid subjectId,
        CancellationToken cancellationToken)
    {
        return McpErrorMapper.ExecuteAsync(() => application.ListGoalsAsync(subjectId, cancellationToken));
    }

    [McpServerTool(Name = "create_goal"), Description("Creates a goal by invoking the application goal use case. Requires goals:write.")]
    public async Task<SubjectGoalDetails> CreateGoalAsync(
        [Description("The subject identifier.")] Guid subjectId,
        CreateSubjectGoalRequest request,
        CancellationToken cancellationToken)
    {
        return await McpErrorMapper.ExecuteAsync(() => application.CreateGoalAsync(subjectId, request, cancellationToken))
            ?? throw new KeyNotFoundException($"Subject '{subjectId}' was not found.");
    }

    [McpServerTool(Name = "complete_goal"), Description("Marks a subject goal complete for its current period. Requires goals:write.")]
    public Task<bool> CompleteGoalAsync(
        [Description("The goal identifier.")] Guid goalId,
        CancellationToken cancellationToken)
    {
        return McpErrorMapper.ExecuteAsync(() => application.CompleteGoalAsync(goalId, cancellationToken));
    }

    [McpServerTool(Name = "list_goal_activity"), Description("Returns goal activity for an inclusive date range. Requires goals:read.")]
    public Task<IReadOnlyCollection<GoalActivityDetails>> ListGoalActivityAsync(
        [Description("The first date in ISO-8601 format.")] DateOnly from,
        [Description("The last date in ISO-8601 format.")] DateOnly to,
        CancellationToken cancellationToken)
    {
        return McpErrorMapper.ExecuteAsync(() => application.ListGoalActivityAsync(from, to, cancellationToken));
    }
}
