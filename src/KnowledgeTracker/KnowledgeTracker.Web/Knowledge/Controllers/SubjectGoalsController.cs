using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Web.Knowledge.Contracts;
using KnowledgeTracker.Web.Knowledge.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeTracker.Web.Knowledge.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class SubjectGoalsController(ISubjectGoalService goals) : ControllerBase
{
    [HttpGet("subjects/{subjectId:guid}/goals")]
    public async Task<ActionResult<IReadOnlyCollection<SubjectGoalResponse>>> ListAsync(Guid subjectId, CancellationToken ct) =>
        Ok((await goals.ListBySubjectAsync(subjectId, ct)).Select(KnowledgeResponseMapper.ToResponse).ToArray());

    [HttpPost("subjects/{subjectId:guid}/goals")]
    public async Task<ActionResult<SubjectGoalResponse>> CreateAsync(Guid subjectId, KnowledgeTracker.Web.Knowledge.Contracts.CreateSubjectGoalRequest request, CancellationToken ct)
    {
        try
        {
            var goal = await goals.CreateAsync(subjectId, new KnowledgeTracker.Application.Knowledge.CreateSubjectGoalRequest(request.Title, request.Kind, request.MetricDefinitionId, request.TargetValue, request.TargetDate, request.Period, request.PeriodStartDate, request.PeriodEndDate), ct);
            return goal is null ? NotFound() : Created($"/api/subject-goals/{goal.Id}", KnowledgeResponseMapper.ToResponse(goal));
        }
        catch (ArgumentException exception) { return BadRequest(new ProblemDetails { Detail = exception.Message }); }
    }

    [HttpDelete("subject-goals/{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct) => await goals.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
