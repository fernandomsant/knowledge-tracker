using System.ComponentModel;
using KnowledgeTracker.Application.Knowledge;
using ModelContextProtocol.Server;

namespace KnowledgeTracker.Mcp;

[McpServerToolType]
public sealed class KnowledgeTools(
    ISubjectService subjects,
    ITopicService topics,
    IStudyNoteService notes,
    ISubjectGoalService goals,
    ISubjectGoalActivityService goalActivity)
{
    [McpServerTool, Description("Lists all subjects in the knowledge tracker.")]
    public Task<IReadOnlyCollection<SubjectSummary>> ListSubjectsAsync(CancellationToken cancellationToken) =>
        subjects.ListAsync(cancellationToken);

    [McpServerTool, Description("Gets one subject, including its notes and layout position.")]
    public async Task<SubjectDetails> GetSubjectAsync(
        [Description("The subject identifier.")] Guid subjectId,
        CancellationToken cancellationToken)
    {
        return await subjects.GetAsync(subjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Subject '{subjectId}' was not found.");
    }

    [McpServerTool, Description("Creates a subject and optionally places it below a parent subject.")]
    public Task<SubjectSummary> CreateSubjectAsync(
        [Description("The subject name.")] string name,
        [Description("An optional subject description.")] string? description,
        [Description("The optional parent subject identifier.")] Guid? parentSubjectId,
        CancellationToken cancellationToken) =>
        subjects.CreateAsync(new CreateSubjectRequest(name, description, parentSubjectId), cancellationToken);

    [McpServerTool, Description("Lists topics that can be used by notes and goals.")]
    public Task<IReadOnlyCollection<TopicDetails>> ListTopicsAsync(CancellationToken cancellationToken) =>
        topics.ListAsync(cancellationToken);

    [McpServerTool, Description("Creates a topic under a subject.")]
    public Task<TopicDetails> CreateTopicAsync(
        [Description("The owning subject identifier.")] Guid subjectId,
        [Description("The topic name.")] string name,
        CancellationToken cancellationToken) =>
        topics.CreateAsync(new CreateTopicRequest(subjectId, name), cancellationToken);

    [McpServerTool, Description("Lists notes directly owned by a subject or, optionally, its descendants.")]
    public Task<IReadOnlyCollection<StudyNoteDetails>> ListNotesAsync(
        [Description("The subject identifier.")] Guid subjectId,
        [Description("When true, include notes owned by descendant subjects.")] bool includeDescendants,
        CancellationToken cancellationToken) =>
        includeDescendants
            ? notes.ListBySubjectTreeAsync(subjectId, cancellationToken)
            : notes.ListBySubjectAsync(subjectId, cancellationToken);

    [McpServerTool, Description("Creates a study note by invoking the application note use case.")]
    public async Task<StudyNoteDetails> CreateNoteAsync(
        [Description("The owning leaf subject identifier.")] Guid subjectId,
        CreateStudyNoteRequest request,
        CancellationToken cancellationToken)
    {
        return await notes.CreateAsync(subjectId, request, cancellationToken)
            ?? throw new KeyNotFoundException($"Subject '{subjectId}' was not found.");
    }

    [McpServerTool, Description("Lists goals belonging to a subject.")]
    public Task<IReadOnlyCollection<SubjectGoalDetails>> ListGoalsAsync(
        [Description("The subject identifier.")] Guid subjectId,
        CancellationToken cancellationToken) =>
        goals.ListBySubjectAsync(subjectId, cancellationToken);

    [McpServerTool, Description("Creates a goal by invoking the application goal use case.")]
    public async Task<SubjectGoalDetails> CreateGoalAsync(
        [Description("The subject identifier.")] Guid subjectId,
        CreateSubjectGoalRequest request,
        CancellationToken cancellationToken)
    {
        return await goals.CreateAsync(subjectId, request, cancellationToken)
            ?? throw new KeyNotFoundException($"Subject '{subjectId}' was not found.");
    }

    [McpServerTool, Description("Marks a subject goal complete for its current period.")]
    public Task<bool> CompleteGoalAsync(
        [Description("The goal identifier.")] Guid goalId,
        CancellationToken cancellationToken) =>
        goals.CompleteAsync(goalId, cancellationToken);

    [McpServerTool, Description("Returns goal activity for an inclusive date range.")]
    public Task<IReadOnlyCollection<GoalActivityDetails>> ListGoalActivityAsync(
        [Description("The first date in ISO-8601 format.")] DateOnly from,
        [Description("The last date in ISO-8601 format.")] DateOnly to,
        CancellationToken cancellationToken) =>
        goalActivity.GetAsync(from, to, cancellationToken);
}
