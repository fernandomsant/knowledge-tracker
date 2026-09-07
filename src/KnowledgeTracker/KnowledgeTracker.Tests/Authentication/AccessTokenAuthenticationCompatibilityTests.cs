using System.Security.Claims;
using System.Text.Encodings.Web;
using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Domain.Authentication;
using KnowledgeTracker.Web.Authentication.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace KnowledgeTracker.Tests.Authentication;

public sealed class AccessTokenAuthenticationCompatibilityTests
{
    [Fact]
    public async Task AuthenticateAsync_ContinuesToAuthenticateNormalApplicationAccessTokens()
    {
        var userId = Guid.NewGuid();
        var accessTokens = new FakeAccessTokenService(userId);
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer normal-token";

        var handler = new AccessTokenAuthenticationHandler(
            new FixedOptionsMonitor<AuthenticationSchemeOptions>(new()),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            accessTokens);

        await handler.InitializeAsync(
            new AuthenticationScheme(
                AccessTokenAuthenticationHandler.AuthenticationScheme,
                null,
                typeof(AccessTokenAuthenticationHandler)),
            context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(userId.ToString(), result.Principal?.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(1, accessTokens.ValidateCalls);
    }

    private sealed class FakeAccessTokenService(Guid userId) : IAccessTokenService
    {
        public int ValidateCalls { get; private set; }

        public AccessToken Issue(AccessToken unsignedToken) => unsignedToken.WithValue("normal-token");

        public AccessToken? Validate(AccessTokenReference token)
        {
            ValidateCalls++;
            return token.Value == "normal-token"
                ? new(
                    new TokenClaims("issuer", userId, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(30), Guid.NewGuid()),
                    Guid.NewGuid(),
                    "knowledge-tracker",
                    token.Value)
                : null;
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
