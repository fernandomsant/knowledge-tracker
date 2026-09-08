using System.Net;
using System.Net.Http.Json;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Mcp.ApplicationApi;
using KnowledgeTracker.Mcp.ApplicationApi.Authentication;
using KnowledgeTracker.Mcp.Configuration;
using Xunit;

namespace KnowledgeTracker.Tests.Authentication;

public sealed class ApplicationApiClientTests
{
    [Fact]
    public async Task ListSubjectsAsync_UsesDedicatedRouteAndMcpBearerToken()
    {
        var subject = new SubjectSummary(Guid.NewGuid(), "C#", null, null, null);
        var transport = new RecordingHttpMessageHandler(_ =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, new[] { subject })));
        var client = CreateClient(transport);

        var result = await client.ListSubjectsAsync(CancellationToken.None);

        Assert.Equal(new[] { subject }, result);
        Assert.NotNull(transport.Request);
        Assert.Equal(HttpMethod.Get, transport.Request.Method);
        Assert.Equal("/mcp-api/subjects", transport.Request.RequestUri?.PathAndQuery);
        Assert.Equal("Bearer", transport.Request.Headers.Authorization?.Scheme);
        Assert.Equal("mcp_identifier_secret", transport.Request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task ListSubjectsAsync_ForwardsConfiguredWorkspaceSelection()
    {
        var workspaceId = Guid.NewGuid();
        var transport = new RecordingHttpMessageHandler(_ =>
            Task.FromResult(JsonResponse(HttpStatusCode.OK, Array.Empty<SubjectSummary>())));
        var client = CreateClient(transport, workspaceId);

        await client.ListSubjectsAsync(CancellationToken.None);

        Assert.Equal(workspaceId.ToString(), transport.Request!.Headers.GetValues("X-Workspace-Id").Single());
    }

    [Fact]
    public async Task CreateSubjectAsync_SerializesTypedRequestAndReadsTypedResponse()
    {
        var subject = new SubjectSummary(Guid.NewGuid(), "C#", "Language notes", null, null);
        var transport = new RecordingHttpMessageHandler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/mcp-api/subjects", request.RequestUri?.PathAndQuery);
            var body = await request.Content!.ReadFromJsonAsync<CreateSubjectRequest>();
            Assert.Equal(new CreateSubjectRequest("C#", "Language notes", null), body);
            return JsonResponse(HttpStatusCode.Created, subject);
        });
        var client = CreateClient(transport);

        var result = await client.CreateSubjectAsync(
            new CreateSubjectRequest("C#", "Language notes", null),
            CancellationToken.None);

        Assert.Equal(subject, result);
    }

    [Fact]
    public async Task GetSubjectAsync_ReturnsNullForNotFound()
    {
        var client = CreateClient(new RecordingHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))));

        var result = await client.GetSubjectAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CompleteGoalAsync_ReturnsFalseForNotFound()
    {
        var client = CreateClient(new RecordingHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))));

        var result = await client.CompleteGoalAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "validation failure")]
    [InlineData(HttpStatusCode.Unauthorized, "authentication failure")]
    [InlineData(HttpStatusCode.Forbidden, "authorization failure")]
    [InlineData(HttpStatusCode.NotFound, "not found")]
    [InlineData(HttpStatusCode.Conflict, "conflict")]
    [InlineData(HttpStatusCode.InternalServerError, "server failure")]
    public async Task ListSubjectsAsync_MapsApplicationErrorsWithoutExposingResponseBody(
        HttpStatusCode statusCode,
        string category)
    {
        const string secretResponseBody = "mcp_real_identifier_real_secret";
        var client = CreateClient(new RecordingHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = JsonContent.Create(new { detail = secretResponseBody }),
            })));

        var exception = await Assert.ThrowsAsync<ApplicationApiException>(() =>
            client.ListSubjectsAsync(CancellationToken.None));

        Assert.Equal((int)statusCode, exception.StatusCode);
        Assert.Equal(category, exception.Category);
        Assert.DoesNotContain(secretResponseBody, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListSubjectsAsync_MapsTransportTimeout()
    {
        var client = CreateClient(new RecordingHttpMessageHandler(_ =>
            throw new TaskCanceledException("simulated timeout", innerException: null, CancellationToken.None)));

        var exception = await Assert.ThrowsAsync<ApplicationApiException>(() =>
            client.ListSubjectsAsync(CancellationToken.None));

        Assert.Equal(0, exception.StatusCode);
        Assert.Equal("timeout", exception.Category);
    }

    [Fact]
    public async Task ListSubjectsAsync_MapsTransportUnavailable()
    {
        var client = CreateClient(new RecordingHttpMessageHandler(_ =>
            throw new HttpRequestException("simulated network failure")));

        var exception = await Assert.ThrowsAsync<ApplicationApiException>(() =>
            client.ListSubjectsAsync(CancellationToken.None));

        Assert.Equal(0, exception.StatusCode);
        Assert.Equal("unavailable", exception.Category);
        Assert.DoesNotContain("simulated network failure", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListSubjectsAsync_PropagatesCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var client = CreateClient(new RecordingHttpMessageHandler(_ =>
            throw new TaskCanceledException("caller cancelled", innerException: null, cancellation.Token)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.ListSubjectsAsync(cancellation.Token));
    }

    [Fact]
    public async Task CreateSubjectAsync_DoesNotRetryFailedMutation()
    {
        var calls = 0;
        var client = CreateClient(new RecordingHttpMessageHandler(_ =>
        {
            calls++;
            throw new HttpRequestException("simulated network failure");
        }));

        await Assert.ThrowsAsync<ApplicationApiException>(() =>
            client.CreateSubjectAsync(new CreateSubjectRequest("C#", null, null), CancellationToken.None));

        Assert.Equal(1, calls);
    }

    private static IApplicationApiClient CreateClient(HttpMessageHandler transport, Guid? workspaceId = null)
    {
        var options = new McpServerOptions
        {
            ListenAddress = "127.0.0.1",
            ListenIpAddress = IPAddress.Loopback,
            Port = 3001,
            McpEndpointPath = "/mcp",
            ApplicationBaseUrl = "http://localhost:5015/",
            AccessToken = "mcp_identifier_secret",
            WorkspaceId = workspaceId,
        };
        var authentication = new McpAccessTokenHandler(options)
        {
            InnerHandler = transport,
        };
        return new ApplicationApiClient(new HttpClient(authentication)
        {
            BaseAddress = new Uri(options.ApplicationBaseUrl),
        });
    }

    private static HttpResponseMessage JsonResponse<T>(HttpStatusCode statusCode, T value) =>
        new(statusCode)
        {
            Content = JsonContent.Create(value),
        };

    private sealed class RecordingHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return responder(request);
        }
    }

}
