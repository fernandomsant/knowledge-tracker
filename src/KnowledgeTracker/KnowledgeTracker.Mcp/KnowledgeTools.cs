using System.ComponentModel;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Domain.Knowledge;
using KnowledgeTracker.Mcp.ApplicationApi;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace KnowledgeTracker.Mcp;

[McpServerToolType]
// MCP protocol boundary: accepts workspace names and delegates with resolved scoped IDs.
public sealed class KnowledgeTools(
    IApplicationApiClient application,
    KnowledgeTracker.Mcp.Configuration.McpServerOptions options)
{
    [McpServerTool(Name = "list_subjects"), Description("Lists all subjects in the knowledge tracker. Requires subjects:read.")]
    public async Task<IReadOnlyCollection<SubjectSummary>> ListSubjectsAsync(
        [Description("The workspace name where this operation must occur.")] string workspaceName,
        CancellationToken cancellationToken)
    {
        return await McpErrorMapper.ExecuteAsync(async () =>
            await application.ListSubjectsAsync(await ResolveWorkspaceIdAsync(workspaceName, cancellationToken), cancellationToken));
    }

    [McpServerTool(Name = "get_subject"), Description("Gets one subject, including its notes and layout position. Requires subjects:read.")]
    public async Task<SubjectDetails> GetSubjectAsync(
        [Description("The workspace name where this operation must occur.")] string workspaceName,
        [Description("The subject identifier.")] Guid subjectId,
        CancellationToken cancellationToken)
    {
        return await McpErrorMapper.ExecuteAsync(async () =>
            await application.GetSubjectAsync(await ResolveWorkspaceIdAsync(workspaceName, cancellationToken), subjectId, cancellationToken))
            ?? throw new KeyNotFoundException($"Subject '{subjectId}' was not found.");
    }

    [McpServerTool(Name = "create_subject"), Description("Creates a subject and optionally places it below a parent subject. Requires subjects:write.")]
    public Task<SubjectSummary> CreateSubjectAsync(
        [Description("The workspace name where this operation must occur.")] string workspaceName,
        [Description("The subject name.")] string name,
        [Description("An optional subject description.")] string? description,
        [Description("The optional parent subject identifier.")] Guid? parentSubjectId,
        CancellationToken cancellationToken)
    {
        return CreateSubjectAsyncCore(workspaceName, name, description, parentSubjectId, cancellationToken);
    }

    [McpServerTool(Name = "list_topics"), Description("Lists topics that can be used by notes and goals. Requires topics:read.")]
    public Task<IReadOnlyCollection<TopicDetails>> ListTopicsAsync(
        [Description("The workspace name where this operation must occur.")] string workspaceName,
        CancellationToken cancellationToken)
    {
        return ListTopicsAsyncCore(workspaceName, cancellationToken);
    }

    [McpServerTool(Name = "create_topic"), Description("Creates a topic under a subject. Requires topics:write.")]
    public Task<TopicDetails> CreateTopicAsync(
        [Description("The workspace name where this operation must occur.")] string workspaceName,
        [Description("The owning subject identifier.")] Guid subjectId,
        [Description("The topic name.")] string name,
        CancellationToken cancellationToken)
    {
        return CreateTopicAsyncCore(workspaceName, subjectId, name, cancellationToken);
    }

    [McpServerTool(Name = "list_notes"), Description("Lists notes directly owned by a subject or, optionally, its descendants. Requires notes:read.")]
    public Task<IReadOnlyCollection<StudyNoteDetails>> ListNotesAsync(
        [Description("The workspace name where this operation must occur.")] string workspaceName,
        [Description("The subject identifier.")] Guid subjectId,
        [Description("When true, include notes owned by descendant subjects.")] bool includeDescendants,
        CancellationToken cancellationToken)
    {
        return ListNotesAsyncCore(workspaceName, subjectId, includeDescendants, cancellationToken);
    }

    [McpServerTool(Name = "create_note"), Description("Creates a study note by invoking the application note use case. Requires notes:write.")]
    public async Task<StudyNoteDetails> CreateNoteAsync(
        [Description("The workspace name where this operation must occur.")] string workspaceName,
        [Description("The owning leaf subject identifier.")] Guid subjectId,
        [Description("The topic identifier for this note.")] Guid topicId,
        [Description("The note title.")] string title,
        [Description("The note content.")] string content,
        [Description("The study duration in whole minutes.")] int studyDurationMinutes,
        [Description("When the study session started, in ISO-8601 format.")] DateTimeOffset studyStartedAtUtc,
        [Description("Optional metric values, each with definitionId and value.")] IReadOnlyCollection<McpStudyNoteMetric>? metrics = null,
        CancellationToken cancellationToken = default)
    {
        return await McpErrorMapper.ExecuteAsync(async () =>
            await application.CreateNoteAsync(
                await ResolveWorkspaceIdAsync(workspaceName, cancellationToken),
                subjectId,
                new CreateStudyNoteRequest(
                    topicId,
                    title,
                    content,
                    TimeSpan.FromMinutes(studyDurationMinutes),
                    studyStartedAtUtc,
                    (metrics ?? []).Select(metric => new StudyNoteMetricRequest(metric.DefinitionId, metric.Value)).ToArray()),
                cancellationToken))
            ?? throw new KeyNotFoundException($"Subject '{subjectId}' was not found.");
    }

    [McpServerTool(Name = "list_goals"), Description("Lists goals belonging to a subject. Requires goals:read.")]
    public Task<IReadOnlyCollection<SubjectGoalDetails>> ListGoalsAsync(
        [Description("The workspace name where this operation must occur.")] string workspaceName,
        [Description("The subject identifier.")] Guid subjectId,
        CancellationToken cancellationToken)
    {
        return ListGoalsAsyncCore(workspaceName, subjectId, cancellationToken);
    }

    [McpServerTool(Name = "create_goal"), Description("Creates a goal by invoking the application goal use case. Requires goals:write.")]
    public async Task<SubjectGoalDetails> CreateGoalAsync(
        [Description("The workspace name where this operation must occur.")] string workspaceName,
        [Description("The subject identifier.")] Guid subjectId,
        [Description("The topic identifier for this goal.")] Guid topicId,
        [Description("The goal title.")] string title,
        [Description("The goal type: MetricTarget or TargetDate.")] GoalKind kind,
        [Description("Required for MetricTarget goals; otherwise omit.")] Guid? metricDefinitionId = null,
        [Description("Required and positive for MetricTarget goals; otherwise omit.")] decimal? targetValue = null,
        [Description("Optional due date for TargetDate goals, in ISO-8601 format.")] DateOnly? targetDate = null,
        [Description("The goal period: AllTime, Daily, Weekly, Monthly, or Custom.")] GoalPeriod period = GoalPeriod.AllTime,
        [Description("Required only when period is Custom, in ISO-8601 format.")] DateOnly? periodStartDate = null,
        [Description("Required only when period is Custom, in ISO-8601 format.")] DateOnly? periodEndDate = null,
        [Description("Optional checklist items for TargetDate goals.")] IReadOnlyCollection<string>? subGoals = null,
        CancellationToken cancellationToken = default)
    {
        return await McpErrorMapper.ExecuteAsync(async () =>
            await application.CreateGoalAsync(
                await ResolveWorkspaceIdAsync(workspaceName, cancellationToken),
                subjectId,
                new CreateSubjectGoalRequest(
                    topicId,
                    title,
                    kind,
                    metricDefinitionId,
                    targetValue,
                    targetDate,
                    period,
                    periodStartDate,
                    periodEndDate,
                    subGoals ?? []),
                cancellationToken))
            ?? throw new KeyNotFoundException($"Subject '{subjectId}' was not found.");
    }

    [McpServerTool(Name = "complete_goal"), Description("Marks a subject goal complete for its current period. Requires goals:write.")]
    public Task<bool> CompleteGoalAsync(
        [Description("The workspace name where this operation must occur.")] string workspaceName,
        [Description("The goal identifier.")] Guid goalId,
        CancellationToken cancellationToken)
    {
        return CompleteGoalAsyncCore(workspaceName, goalId, cancellationToken);
    }

    [McpServerTool(Name = "list_goal_activity"), Description("Returns goal activity for an inclusive date range. Requires goals:read.")]
    public Task<IReadOnlyCollection<GoalActivityDetails>> ListGoalActivityAsync(
        [Description("The workspace name where this operation must occur.")] string workspaceName,
        [Description("The first date in ISO-8601 format.")] DateOnly from,
        [Description("The last date in ISO-8601 format.")] DateOnly to,
        CancellationToken cancellationToken)
    {
        return ListGoalActivityAsyncCore(workspaceName, from, to, cancellationToken);
    }

    private async Task<SubjectSummary> CreateSubjectAsyncCore(
        string workspaceName,
        string name,
        string? description,
        Guid? parentSubjectId,
        CancellationToken cancellationToken) =>
        await McpErrorMapper.ExecuteAsync(async () =>
            await application.CreateSubjectAsync(
                await ResolveWorkspaceIdAsync(workspaceName, cancellationToken),
                new CreateSubjectRequest(name, description, parentSubjectId),
                cancellationToken));

    private async Task<IReadOnlyCollection<TopicDetails>> ListTopicsAsyncCore(
        string workspaceName,
        CancellationToken cancellationToken) =>
        await McpErrorMapper.ExecuteAsync(async () =>
            await application.ListTopicsAsync(await ResolveWorkspaceIdAsync(workspaceName, cancellationToken), cancellationToken));

    private async Task<TopicDetails> CreateTopicAsyncCore(
        string workspaceName,
        Guid subjectId,
        string name,
        CancellationToken cancellationToken) =>
        await McpErrorMapper.ExecuteAsync(async () =>
            await application.CreateTopicAsync(
                await ResolveWorkspaceIdAsync(workspaceName, cancellationToken),
                subjectId,
                name,
                cancellationToken));

    private async Task<IReadOnlyCollection<StudyNoteDetails>> ListNotesAsyncCore(
        string workspaceName,
        Guid subjectId,
        bool includeDescendants,
        CancellationToken cancellationToken) =>
        await McpErrorMapper.ExecuteAsync(async () =>
            await application.ListNotesAsync(
                await ResolveWorkspaceIdAsync(workspaceName, cancellationToken),
                subjectId,
                includeDescendants,
                cancellationToken));

    private async Task<IReadOnlyCollection<SubjectGoalDetails>> ListGoalsAsyncCore(
        string workspaceName,
        Guid subjectId,
        CancellationToken cancellationToken) =>
        await McpErrorMapper.ExecuteAsync(async () =>
            await application.ListGoalsAsync(
                await ResolveWorkspaceIdAsync(workspaceName, cancellationToken),
                subjectId,
                cancellationToken));

    private async Task<bool> CompleteGoalAsyncCore(
        string workspaceName,
        Guid goalId,
        CancellationToken cancellationToken) =>
        await McpErrorMapper.ExecuteAsync(async () =>
            await application.CompleteGoalAsync(
                await ResolveWorkspaceIdAsync(workspaceName, cancellationToken),
                goalId,
                cancellationToken));

    private async Task<IReadOnlyCollection<GoalActivityDetails>> ListGoalActivityAsyncCore(
        string workspaceName,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        await McpErrorMapper.ExecuteAsync(async () =>
            await application.ListGoalActivityAsync(
                await ResolveWorkspaceIdAsync(workspaceName, cancellationToken),
                from,
                to,
                cancellationToken));

    private async Task<Guid> ResolveWorkspaceIdAsync(string workspaceName, CancellationToken cancellationToken)
    {
        var normalizedName = workspaceName?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
            throw new McpException("A workspace name must be specified for every MCP operation.");

        if (options.WorkspaceNames.Count > 0
            && !options.WorkspaceNames.Contains(normalizedName, StringComparer.OrdinalIgnoreCase))
            throw new McpException($"The MCP server is not configured to access workspace '{normalizedName}'.");

        var workspace = (await application.ListWorkspacesAsync(cancellationToken))
            .SingleOrDefault(candidate => string.Equals(candidate.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
        return workspace?.Id
            ?? throw new McpException($"Workspace '{normalizedName}' was not found for the authenticated user.");
    }
}
