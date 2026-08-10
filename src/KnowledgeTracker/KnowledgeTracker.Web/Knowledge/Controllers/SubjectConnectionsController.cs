using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Web.Knowledge.Contracts;
using KnowledgeTracker.Web.Knowledge.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CreateSubjectConnectionHttpRequest = KnowledgeTracker.Web.Knowledge.Contracts.CreateSubjectConnectionRequest;

namespace KnowledgeTracker.Web.Knowledge.Controllers;

[ApiController]
[Authorize]
public sealed class SubjectConnectionsController(
    ISubjectService subjects,
    ISubjectConnectionService connections
) : ControllerBase
{
    private const string ListSubjectConnectionsRoute = "list-subject-connections";

    [HttpGet("api/subjects/{subjectId:guid}/connections", Name = ListSubjectConnectionsRoute)]
    [ProducesResponseType(typeof(IReadOnlyCollection<SubjectConnectionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<SubjectConnectionResponse>>> ListBySubjectAsync(
        Guid subjectId,
        CancellationToken ct
    )
    {
        if (await subjects.GetAsync(subjectId, ct) is null)
            return NotFound();

        return Ok(
            (await connections.ListBySubjectAsync(subjectId, ct))
                .Select(KnowledgeResponseMapper.ToResponse)
                .ToArray()
        );
    }

    [HttpPost("api/subject-connections")]
    [ProducesResponseType(typeof(SubjectConnectionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubjectConnectionResponse>> CreateAsync(
        CreateSubjectConnectionHttpRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var connection = await connections.CreateAsync(
                new KnowledgeTracker.Application.Knowledge.CreateSubjectConnectionRequest(
                    request.SubjectId,
                    request.ConnectedSubjectId
                ),
                ct
            );
            if (connection is null)
                return NotFound();

            var response = KnowledgeResponseMapper.ToResponse(connection);
            return CreatedAtRoute(
                ListSubjectConnectionsRoute,
                new { subjectId = response.SubjectId },
                response
            );
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Detail = exception.Message });
        }
    }

    [HttpDelete("api/subject-connections/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct) =>
        await connections.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
