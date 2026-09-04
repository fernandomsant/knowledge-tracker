using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Web.Authentication.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeTracker.Web.Authentication.Controllers;

[ApiController, Authorize, Route("api/mcp-access-tokens")]
public sealed class McpAccessTokensController(IMcpAccessTokenService tokens) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateMcpAccessTokenHttpResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateMcpAccessTokenHttpResponse>> CreateAsync(
        CreateMcpAccessTokenHttpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await tokens.CreateAsync(
                new(request.Name, request.ExpiresAtUtc, request.Scopes),
                cancellationToken);
            return Created($"/api/mcp-access-tokens/{result.Id}", new CreateMcpAccessTokenHttpResponse(
                result.Id,
                result.Name,
                result.AccessToken,
                result.CreatedAtUtc,
                result.ExpiresAtUtc,
                result.Scopes));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid MCP access token request.", Detail = exception.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<McpAccessTokenHttpResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<McpAccessTokenHttpResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await tokens.ListAsync(cancellationToken);
            return Ok(result.Select(Map).ToArray());
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return await tokens.RevokeAsync(id, cancellationToken) ? NoContent() : NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private static McpAccessTokenHttpResponse Map(McpAccessTokenDto token) => new(
        token.Id,
        token.Name,
        token.CreatedAtUtc,
        token.ExpiresAtUtc,
        token.RevokedAtUtc,
        token.LastUsedAtUtc,
        token.Scopes);
}
