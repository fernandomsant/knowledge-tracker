using ApplicationKnowledge = KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Web.Authentication.Services;
using KnowledgeTracker.Web.Knowledge.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeTracker.Web.Knowledge.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = McpAccessTokenAuthenticationHandler.AuthenticationScheme)]
[ServiceFilter(typeof(McpExceptionFilter))]
[Route("mcp-api")]
public sealed class McpKnowledgeController(
    ApplicationKnowledge.ISubjectService subjects,
    ApplicationKnowledge.ITopicService topics,
    ApplicationKnowledge.IStudyNoteService notes,
    ApplicationKnowledge.ISubjectGoalService goals,
    ApplicationKnowledge.ISubjectGoalActivityService goalActivity,
    ApplicationKnowledge.IWorkspaceService workspaces) : ControllerBase
{
    // Workspace names are resolved through this MCP-authenticated route before
    // knowledge operations use the existing workspace ID data scope.
    [HttpGet("workspaces")]
    public async Task<ActionResult<IReadOnlyCollection<ApplicationKnowledge.WorkspaceDetails>>> ListWorkspacesAsync(
        CancellationToken ct) =>
        Ok(await workspaces.ListAsync(ct));

    [HttpGet("subjects")]
    public async Task<ActionResult<IReadOnlyCollection<ApplicationKnowledge.SubjectSummary>>> ListSubjectsAsync(CancellationToken ct) =>
        Ok(await subjects.ListAsync(ct));

    [HttpGet("subjects/{id:guid}")]
    public async Task<ActionResult<ApplicationKnowledge.SubjectDetails>> GetSubjectAsync(Guid id, CancellationToken ct)
    {
        var subject = await subjects.GetAsync(id, ct);
        return subject is null ? NotFound() : Ok(subject);
    }

    [HttpPost("subjects")]
    public async Task<ActionResult<ApplicationKnowledge.SubjectSummary>> CreateSubjectAsync(
        ApplicationKnowledge.CreateSubjectRequest request,
        CancellationToken ct)
    {
        var subject = await subjects.CreateAsync(request, ct);
        return Created($"/mcp-api/subjects/{subject.Id}", subject);
    }

    [HttpGet("topics")]
    public async Task<ActionResult<IReadOnlyCollection<ApplicationKnowledge.TopicDetails>>> ListTopicsAsync(CancellationToken ct) =>
        Ok(await topics.ListAsync(ct));

    [HttpPost("subjects/{subjectId:guid}/topics")]
    public async Task<ActionResult<ApplicationKnowledge.TopicDetails>> CreateTopicAsync(
        Guid subjectId,
        ApplicationKnowledge.CreateTopicRequest request,
        CancellationToken ct)
    {
        var topic = await topics.CreateAsync(request with { SubjectId = subjectId }, ct);
        return Created($"/mcp-api/topics/{topic.Id}", topic);
    }

    [HttpGet("subjects/{subjectId:guid}/notes")]
    public async Task<ActionResult<IReadOnlyCollection<ApplicationKnowledge.StudyNoteDetails>>> ListNotesAsync(
        Guid subjectId,
        [FromQuery] bool includeDescendants,
        CancellationToken ct)
    {
        var result = includeDescendants
            ? await notes.ListBySubjectTreeAsync(subjectId, ct)
            : await notes.ListBySubjectAsync(subjectId, ct);
        return Ok(result);
    }

    [HttpPost("subjects/{subjectId:guid}/notes")]
    public async Task<ActionResult<ApplicationKnowledge.StudyNoteDetails>> CreateNoteAsync(
        Guid subjectId,
        ApplicationKnowledge.CreateStudyNoteRequest request,
        CancellationToken ct)
    {
        var note = await notes.CreateAsync(subjectId, request, ct);
        return note is null ? NotFound() : Created($"/mcp-api/study-notes/{note.Id}", note);
    }

    [HttpGet("subjects/{subjectId:guid}/goals")]
    public async Task<ActionResult<IReadOnlyCollection<ApplicationKnowledge.SubjectGoalDetails>>> ListGoalsAsync(
        Guid subjectId,
        CancellationToken ct) => Ok(await goals.ListBySubjectAsync(subjectId, ct));

    [HttpPost("subjects/{subjectId:guid}/goals")]
    public async Task<ActionResult<ApplicationKnowledge.SubjectGoalDetails>> CreateGoalAsync(
        Guid subjectId,
        ApplicationKnowledge.CreateSubjectGoalRequest request,
        CancellationToken ct)
    {
        var goal = await goals.CreateAsync(subjectId, request, ct);
        return goal is null ? NotFound() : Created($"/mcp-api/subject-goals/{goal.Id}", goal);
    }

    [HttpPost("subject-goals/{id:guid}/complete")]
    public async Task<IActionResult> CompleteGoalAsync(Guid id, CancellationToken ct) =>
        await goals.CompleteAsync(id, ct) ? NoContent() : NotFound();

    [HttpGet("goal-activity")]
    public async Task<ActionResult<IReadOnlyCollection<ApplicationKnowledge.GoalActivityDetails>>> ListGoalActivityAsync(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct) => Ok(await goalActivity.GetAsync(from, to, ct));
}
