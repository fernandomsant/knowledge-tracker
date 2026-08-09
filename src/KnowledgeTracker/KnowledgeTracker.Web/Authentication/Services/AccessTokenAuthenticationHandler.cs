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
    IAccessTokenService accessTokens
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "AccessToken";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var value = authorization["Bearer ".Length..].Trim();
        AccessToken? token;
        try
        {
            token = accessTokens.Validate(new AccessTokenReference(value));
        }
        catch (ArgumentException)
        {
            return Task.FromResult(AuthenticateResult.Fail("The access token is malformed."));
        }

        if (token is null)
            return Task.FromResult(AuthenticateResult.Fail("The access token is invalid."));

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, token.Claims.Subject.ToString()),
                new Claim("session_id", token.SessionId.ToString()),
                new Claim("nonce", token.Claims.Nonce.ToString()),
            ],
            AuthenticationScheme
        );
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(
            AuthenticateResult.Success(new AuthenticationTicket(principal, AuthenticationScheme))
        );
    }
}