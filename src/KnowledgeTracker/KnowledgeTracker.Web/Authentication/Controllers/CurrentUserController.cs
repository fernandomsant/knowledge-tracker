using System.Security.Claims;
using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Web.Authentication.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeTracker.Web.Authentication.Controllers;

[ApiController]
[Authorize]
[Route("api/current-user")]
public sealed class CurrentUserController(ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentUserResponse>> GetAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var id))
            return Unauthorized();

        var user = await currentUser.GetAsync(id, ct);
        return user is null ? NotFound() : Ok(new CurrentUserResponse(user.Id, user.Login));
    }
}
