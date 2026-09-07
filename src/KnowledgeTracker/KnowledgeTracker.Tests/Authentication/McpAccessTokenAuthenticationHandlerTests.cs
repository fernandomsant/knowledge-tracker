using System.Security.Claims;
using System.Text.Encodings.Web;
using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Web.Authentication.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace KnowledgeTracker.Tests.Authentication;

public sealed class McpAccessTokenAuthenticationHandlerTests
{
    [Fact]
    public async Task AuthenticateAsync_RejectsNormalApplicationAccessToken()
    {
        var handler = CreateHandler(new McpAccessTokenValidationResult(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ["subjects:read"]));
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer normal-token";

        await InitializeAsync(handler, context);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AuthenticateAsync_UsesValidatedMcpTokenAndPersistsItsScopesAsClaims()
    {
        var userId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var validator = new FakeMcpAccessTokenValidator(new(
            userId,
            tokenId,
            ["subjects:read", "subjects:write"]));
        var handler = new McpAccessTokenAuthenticationHandler(
            new FixedOptionsMonitor<AuthenticationSchemeOptions>(new()),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            validator);
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer mcp_identifier_secret";

        await InitializeAsync(handler, context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(userId.ToString(), result.Principal?.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(tokenId.ToString(), result.Principal?.FindFirstValue("mcp_access_token_id"));
        Assert.Equal("mcp_access_token", result.Principal?.FindFirstValue("authentication_method"));
        Assert.Equal(["subjects:read", "subjects:write"], result.Principal?.FindAll("mcp_scope").Select(claim => claim.Value));
        Assert.Equal("mcp_identifier_secret", validator.ValidatedToken);
    }

    private static McpAccessTokenAuthenticationHandler CreateHandler(McpAccessTokenValidationResult result) =>
        new(
            new FixedOptionsMonitor<AuthenticationSchemeOptions>(new()),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            new FakeMcpAccessTokenValidator(result));

    private static Task InitializeAsync(
        McpAccessTokenAuthenticationHandler handler,
        HttpContext context) =>
        handler.InitializeAsync(new AuthenticationScheme(
            McpAccessTokenAuthenticationHandler.AuthenticationScheme,
            null,
            typeof(McpAccessTokenAuthenticationHandler)), context);

    private sealed class FakeMcpAccessTokenValidator(McpAccessTokenValidationResult result) : IMcpAccessTokenValidator
    {
        public string? ValidatedToken { get; private set; }

        public Task<McpAccessTokenValidationResult?> ValidateAsync(string accessToken, CancellationToken ct)
        {
            ValidatedToken = accessToken;
            return Task.FromResult<McpAccessTokenValidationResult?>(result);
        }
    }

    private sealed class FixedOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable OnChange(Action<T, string?> listener) => EmptyDisposable.Instance;

        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new();
            public void Dispose() { }
        }
    }
}
