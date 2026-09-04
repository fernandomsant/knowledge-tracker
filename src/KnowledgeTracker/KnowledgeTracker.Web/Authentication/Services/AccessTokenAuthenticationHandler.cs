using System.Security.Claims;
using System.Text.Encodings.Web;
using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Domain.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace KnowledgeTracker.Web.Authentication.Services;

public sealed class AccessTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IAccessTokenService accessTokens,
    IMcpAccessTokenValidator mcpAccessTokens
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "AccessToken";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var value = authorization["Bearer ".Length..].Trim();
        if (value.StartsWith("mcp_", StringComparison.Ordinal))
        {
            var mcpToken = await mcpAccessTokens.ValidateAsync(value, Context.RequestAborted);
            if (mcpToken is null)
                return AuthenticateResult.Fail("The MCP access token is invalid.");

            var mcpClaims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, mcpToken.UserId.ToString()),
                new("authentication_method", "mcp_access_token"),
                new("mcp_access_token_id", mcpToken.TokenId.ToString()),
            };
            mcpClaims.AddRange(mcpToken.DelegatedScopes.Select(scope => new Claim("mcp_delegated_scope", scope)));
            mcpClaims.AddRange(mcpToken.Scopes.Select(scope => new Claim("mcp_scope", scope)));
            return AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(mcpClaims, AuthenticationScheme)),
                AuthenticationScheme));
        }

        AccessToken? token;
        try
        {
            token = accessTokens.Validate(new AccessTokenReference(value));
        }
        catch (ArgumentException)
        {
            return AuthenticateResult.Fail("The access token is malformed.");
        }

        if (token is null)
            return AuthenticateResult.Fail("The access token is invalid.");

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, token.Claims.Subject.ToString()),
                new Claim("session_id", token.SessionId.ToString()),
                new Claim("nonce", token.Claims.Nonce.ToString()),
            ],
            AuthenticationScheme
        );
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, AuthenticationScheme));
    }
}
