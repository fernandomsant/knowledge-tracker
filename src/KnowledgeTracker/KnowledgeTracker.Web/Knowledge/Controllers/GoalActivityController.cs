using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Web.Knowledge.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeTracker.Web.Knowledge.Controllers;

[ApiController]
[Authorize]
[Route("api/goal-activity")]
public sealed class GoalActivityController(ISubjectGoalActivityService activity) : ControllerBase
{
    /// <summary>Returns expected goal occurrences and persisted completion timestamps for a UTC date range.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<GoalActivityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<GoalActivityResponse>>> GetAsync(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct)
    {
        try
        {
            var rows = await activity.GetAsync(from, to, ct);
            return Ok(rows.Select(row => new GoalActivityResponse(row.GoalId, row.SubjectId, row.TopicId, row.GoalTitle, row.OccurrenceStartDate, row.OccurrenceEndDate, row.CompletedAtUtc)).ToArray());
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
    }
}
