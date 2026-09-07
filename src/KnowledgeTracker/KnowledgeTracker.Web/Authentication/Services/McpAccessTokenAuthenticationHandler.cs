using System.Security.Claims;
using System.Text.Encodings.Web;
using KnowledgeTracker.Application.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace KnowledgeTracker.Web.Authentication.Services;

public sealed class McpAccessTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IMcpAccessTokenValidator tokens
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "McpAccessToken";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var value = authorization["Bearer ".Length..].Trim();
        if (!value.StartsWith("mcp_", StringComparison.Ordinal))
            return AuthenticateResult.Fail("The MCP access token is invalid.");

        var token = await tokens.ValidateAsync(value, Context.RequestAborted);
        if (token is null)
            return AuthenticateResult.Fail("The MCP access token is invalid.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, token.UserId.ToString()),
            new("authentication_method", "mcp_access_token"),
            new("mcp_access_token_id", token.TokenId.ToString()),
        };
        claims.AddRange(token.Scopes.Select(scope => new Claim("mcp_scope", scope)));

        return AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationScheme)),
            AuthenticationScheme));
    }
}
