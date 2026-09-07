using System.ComponentModel;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Mcp.ApplicationApi;
using ModelContextProtocol.Server;

namespace KnowledgeTracker.Mcp;

[McpServerToolType]
public sealed class KnowledgeTools(
    IApplicationApiClient application)
{
    [McpServerTool, Description("Lists all subjects in the knowledge tracker. Requires subjects:read.")]
    public Task<IReadOnlyCollection<SubjectSummary>> ListSubjectsAsync(CancellationToken cancellationToken)
    {
        return application.ListSubjectsAsync(cancellationToken);
    }

    [McpServerTool, Description("Gets one subject, including its notes and layout position. Requires subjects:read.")]
    public async Task<SubjectDetails> GetSubjectAsync(
        [Description("The subject identifier.")] Guid subjectId,
        CancellationToken cancellationToken)
    {
        return await application.GetSubjectAsync(subjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Subject '{subjectId}' was not found.");
    }

    [McpServerTool, Description("Creates a subject and optionally places it below a parent subject. Requires subjects:write.")]
    public Task<SubjectSummary> CreateSubjectAsync(
        [Description("The subject name.")] string name,
        [Description("An optional subject description.")] string? description,
        [Description("The optional parent subject identifier.")] Guid? parentSubjectId,
        CancellationToken cancellationToken)
    {
        return application.CreateSubjectAsync(new CreateSubjectRequest(name, description, parentSubjectId), cancellationToken);
    }

    [McpServerTool, Description("Lists topics that can be used by notes and goals. Requires topics:read.")]
    public Task<IReadOnlyCollection<TopicDetails>> ListTopicsAsync(CancellationToken cancellationToken)
    {
        return application.ListTopicsAsync(cancellationToken);
    }

    [McpServerTool, Description("Creates a topic under a subject. Requires topics:write.")]
    public Task<TopicDetails> CreateTopicAsync(
        [Description("The owning subject identifier.")] Guid subjectId,
        [Description("The topic name.")] string name,
        CancellationToken cancellationToken)
    {
        return application.CreateTopicAsync(subjectId, name, cancellationToken);
    }

    [McpServerTool, Description("Lists notes directly owned by a subject or, optionally, its descendants. Requires notes:read.")]
    public Task<IReadOnlyCollection<StudyNoteDetails>> ListNotesAsync(
        [Description("The subject identifier.")] Guid subjectId,
        [Description("When true, include notes owned by descendant subjects.")] bool includeDescendants,
        CancellationToken cancellationToken)
    {
        return application.ListNotesAsync(subjectId, includeDescendants, cancellationToken);
    }

    [McpServerTool, Description("Creates a study note by invoking the application note use case. Requires notes:write.")]
    public async Task<StudyNoteDetails> CreateNoteAsync(
        [Description("The owning leaf subject identifier.")] Guid subjectId,
        CreateStudyNoteRequest request,
        CancellationToken cancellationToken)
    {
        return await application.CreateNoteAsync(subjectId, request, cancellationToken)
            ?? throw new KeyNotFoundException($"Subject '{subjectId}' was not found.");
    }

    [McpServerTool, Description("Lists goals belonging to a subject. Requires goals:read.")]
    public Task<IReadOnlyCollection<SubjectGoalDetails>> ListGoalsAsync(
        [Description("The subject identifier.")] Guid subjectId,
        CancellationToken cancellationToken)
    {
        return application.ListGoalsAsync(subjectId, cancellationToken);
    }

    [McpServerTool, Description("Creates a goal by invoking the application goal use case. Requires goals:write.")]
    public async Task<SubjectGoalDetails> CreateGoalAsync(
        [Description("The subject identifier.")] Guid subjectId,
        CreateSubjectGoalRequest request,
        CancellationToken cancellationToken)
    {
        return await application.CreateGoalAsync(subjectId, request, cancellationToken)
            ?? throw new KeyNotFoundException($"Subject '{subjectId}' was not found.");
    }

    [McpServerTool, Description("Marks a subject goal complete for its current period. Requires goals:write.")]
    public Task<bool> CompleteGoalAsync(
        [Description("The goal identifier.")] Guid goalId,
        CancellationToken cancellationToken)
    {
        return application.CompleteGoalAsync(goalId, cancellationToken);
    }

    [McpServerTool, Description("Returns goal activity for an inclusive date range. Requires goals:read.")]
    public Task<IReadOnlyCollection<GoalActivityDetails>> ListGoalActivityAsync(
        [Description("The first date in ISO-8601 format.")] DateOnly from,
        [Description("The last date in ISO-8601 format.")] DateOnly to,
        CancellationToken cancellationToken)
    {
        return application.ListGoalActivityAsync(from, to, cancellationToken);
    }
}
