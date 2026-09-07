using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Web.Authentication.Contracts;
using KnowledgeTracker.Web.Authentication.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace KnowledgeTracker.Tests.Authentication;

public sealed class McpAccessTokensControllerTests
{
    [Fact]
    public async Task CreateAsync_ReturnsRawTokenOnlyInCreationResponse()
    {
        var service = new FakeTokenService
        {
            Created = new CreateMcpAccessTokenResult(
                Guid.NewGuid(), "desktop", "mcp_identifier_secret", DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(1), ["notes:read"])
        };
        var controller = new McpAccessTokensController(service);

        var result = await controller.CreateAsync(
            new() { Name = "desktop", ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(1), Scopes = ["notes:read"] },
            CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result.Result);
        var response = Assert.IsType<CreateMcpAccessTokenHttpResponse>(created.Value);
        Assert.Equal("mcp_identifier_secret", response.AccessToken);
    }

    [Fact]
    public async Task CreateAsync_MapsUnauthorizedScopeToForbidden()
    {
        var controller = new McpAccessTokensController(new FakeTokenService
        {
            CreateException = new UnauthorizedAccessException()
        });

        var result = await controller.CreateAsync(
            new() { Name = "desktop", ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(1), Scopes = ["subjects:write"] },
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task ListAsync_ReturnsMetadataWithoutSecretFields()
    {
        var controller = new McpAccessTokensController(new FakeTokenService
        {
            Listed = [new McpAccessTokenDto(Guid.NewGuid(), "desktop", DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1), null, null, ["notes:read"])]
        });

        var result = await controller.ListAsync(CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var token = Assert.Single(Assert.IsAssignableFrom<IReadOnlyCollection<McpAccessTokenHttpResponse>>(response.Value));
        Assert.Equal("desktop", token.Name);
        Assert.DoesNotContain("Secret", string.Join(',', token.GetType().GetProperties().Select(property => property.Name)), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeTokenService : IMcpAccessTokenService
    {
        public CreateMcpAccessTokenResult? Created { get; init; }
        public Exception? CreateException { get; init; }
        public IReadOnlyCollection<McpAccessTokenDto> Listed { get; init; } = [];

        public Task<CreateMcpAccessTokenResult> CreateAsync(CreateMcpAccessTokenRequest request, CancellationToken ct) =>
            CreateException is not null ? Task.FromException<CreateMcpAccessTokenResult>(CreateException) : Task.FromResult(Created!);
        public Task<IReadOnlyCollection<McpAccessTokenDto>> ListAsync(CancellationToken ct) => Task.FromResult(Listed);
        public Task<bool> RevokeAsync(Guid id, CancellationToken ct) => Task.FromResult(false);
    }
}
