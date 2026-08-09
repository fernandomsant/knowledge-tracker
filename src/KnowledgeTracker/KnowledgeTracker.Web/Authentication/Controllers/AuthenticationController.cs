using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Domain.Authentication;
using KnowledgeTracker.Web.Authentication.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeTracker.Web.Authentication.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/authentication")]
public sealed class AuthenticationController(IAuthenticationService authentication) : ControllerBase
{
    private const string RefreshTokenCookieName = "knowledge_tracker_refresh";

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        try
        {
            await authentication.RegisterAsync(request.Login, request.Password, ct);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return Conflict();
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken ct
    )
    {
        var tokens = await authentication.AuthenticateAsync(
            request.Login,
            request.Password,
            new RequestContext(
                UserAgent(),
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0",
                HttpContext.Connection.RemotePort
            ),
            ct
        );
        if (tokens is null)
            return Unauthorized();

        SetRefreshTokenCookie(tokens.RefreshToken);
        return Ok(AuthenticationResponse.From(tokens));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResponse>> RefreshAsync(
        CancellationToken ct
    )
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized();

        try
        {
            var tokens = await authentication.RefreshAsync(new RefreshToken(refreshToken), ct);
            if (tokens is null)
            {
                Response.Cookies.Delete(RefreshTokenCookieName, CookieOptions());
                return Unauthorized();
            }

            SetRefreshTokenCookie(tokens.RefreshToken);
            return Ok(AuthenticationResponse.From(tokens));
        }
        catch (ArgumentException)
        {
            Response.Cookies.Delete(RefreshTokenCookieName, CookieOptions());
            return Unauthorized();
        }
    }

    private string UserAgent()
    {
        var value = Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(value)
            ? "unknown"
            : value[..Math.Min(value.Length, 1024)];
    }

    private void SetRefreshTokenCookie(RefreshToken refreshToken) =>
        Response.Cookies.Append(RefreshTokenCookieName, refreshToken.Value, CookieOptions());

    private CookieOptions CookieOptions() =>
        new()
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Secure = Request.IsHttps,
            Path = "/api/authentication",
            MaxAge = TimeSpan.FromHours(5),
        };
}
