using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Web.Authentication.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CreateWorkspaceApplicationRequest = KnowledgeTracker.Application.Knowledge.CreateWorkspaceRequest;
using CreateWorkspaceHttpRequest = KnowledgeTracker.Web.Authentication.Contracts.CreateWorkspaceRequest;

namespace KnowledgeTracker.Web.Authentication.Controllers;

[ApiController]
[Authorize]
[Route("api/workspaces")]
public sealed class WorkspacesController(IWorkspaceService workspaces) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<WorkspaceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<WorkspaceResponse>>> ListAsync(CancellationToken ct) =>
        Ok((await workspaces.ListAsync(ct)).Select(ToResponse).ToArray());

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkspaceResponse>> GetAsync(Guid id, CancellationToken ct)
    {
        var workspace = await workspaces.GetAsync(id, ct);
        return workspace is null ? NotFound() : Ok(ToResponse(workspace));
    }

    [HttpPost]
    [ProducesResponseType(typeof(WorkspaceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkspaceResponse>> CreateAsync(
        CreateWorkspaceHttpRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var workspace = await workspaces.CreateAsync(
                new CreateWorkspaceApplicationRequest(request.Name),
                ct
            );
            var response = ToResponse(workspace);
            return CreatedAtAction(nameof(GetAsync), new { id = response.Id }, response);
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

    private static WorkspaceResponse ToResponse(WorkspaceDetails workspace) =>
        new(workspace.Id, workspace.Name, workspace.CreatedAtUtc);
}
