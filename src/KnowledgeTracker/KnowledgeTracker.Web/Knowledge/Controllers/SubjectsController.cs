using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Web.Knowledge.Contracts;
using KnowledgeTracker.Web.Knowledge.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CreateSubjectHttpRequest = KnowledgeTracker.Web.Knowledge.Contracts.CreateSubjectRequest;
using UpdateSubjectHttpRequest = KnowledgeTracker.Web.Knowledge.Contracts.UpdateSubjectRequest;

namespace KnowledgeTracker.Web.Knowledge.Controllers;

[ApiController]
[Authorize]
[Route("api/subjects")]
public sealed class SubjectsController(ISubjectService subjects) : ControllerBase
{
    private const string GetSubjectByIdRoute = "get-subject-by-id";

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<SubjectSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SubjectSummaryResponse>>> ListAsync(
        CancellationToken ct
    ) =>
        Ok((await subjects.ListAsync(ct)).Select(KnowledgeResponseMapper.ToResponse).ToArray());

    [HttpGet("{id:guid}", Name = GetSubjectByIdRoute)]
    [ProducesResponseType(typeof(SubjectDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectDetailsResponse>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var subject = await subjects.GetAsync(id, ct);
        return subject is null ? NotFound() : Ok(KnowledgeResponseMapper.ToResponse(subject));
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubjectSummaryResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<SubjectSummaryResponse>> CreateAsync(
        CreateSubjectHttpRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var subject = await subjects.CreateAsync(
                new KnowledgeTracker.Application.Knowledge.CreateSubjectRequest(
                    request.Name,
                    request.Description,
                    request.ParentSubjectId
                ),
                ct
            );
            var response = KnowledgeResponseMapper.ToResponse(subject);
            return CreatedAtRoute(GetSubjectByIdRoute, new { id = response.Id }, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SubjectSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectSummaryResponse>> UpdateAsync(
        Guid id,
        UpdateSubjectHttpRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var subject = await subjects.UpdateAsync(
                id,
                new KnowledgeTracker.Application.Knowledge.UpdateSubjectRequest(
                    request.Name,
                    request.Description,
                    request.ParentSubjectId
                ),
                ct
            );
            return subject is null ? NotFound() : Ok(KnowledgeResponseMapper.ToResponse(subject));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct) =>
        await subjects.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
