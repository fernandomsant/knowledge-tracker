using System.Net;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Mcp;
using KnowledgeTracker.Mcp.ApplicationApi;
using KnowledgeTracker.Mcp.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using Xunit;

namespace KnowledgeTracker.Tests.Mcp;

public sealed class McpHttpIntegrationTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private const string WorkspaceName = "Personal";

    [Fact]
    public async Task ListSubjects_UsesOfficialStreamableHttpTransportAndRegisteredTool()
    {
        var subjects = new[]
        {
            new SubjectSummary(Guid.NewGuid(), "C#", null, null, null),
        };
        var application = new StubApplicationApiClient(subjects);
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddSingleton(new KnowledgeTracker.Mcp.Configuration.McpServerOptions
        {
            ListenAddress = "127.0.0.1",
            ListenIpAddress = IPAddress.Loopback,
            Port = 3001,
            McpEndpointPath = "/mcp",
            ApplicationBaseUrl = "http://localhost:5015/",
            AccessToken = "mcp_identifier_secret",
            WorkspaceNames = [],
        });
        builder.Services.AddSingleton<IApplicationApiClient>(application);
        builder.Services
            .AddMcpServer()
            .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
            .WithTools<KnowledgeTools>();

        await using var server = builder.Build();
        server.MapMcp("/mcp");
        await server.StartAsync();

        var serverAddress = server.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()!
            .Addresses
            .Single();
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(serverAddress.TrimEnd('/') + "/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
        });

        await using var client = await McpClient.CreateAsync(transport);
        var tools = await client.ListToolsAsync();
        var listSubjects = Assert.Single(tools, tool => tool.Name == "list_subjects");
        var result = await listSubjects.CallAsync(new Dictionary<string, object?>
        {
            ["workspaceName"] = WorkspaceName,
        });

        Assert.NotEqual(true, result.IsError);
        Assert.NotEmpty(result.Content);
        Assert.True(application.ListSubjectsCalled);

        await server.StopAsync();
    }

    private sealed class StubApplicationApiClient(IReadOnlyCollection<SubjectSummary> subjects) : IApplicationApiClient
    {
        public bool ListSubjectsCalled { get; private set; }

        public Task<IReadOnlyCollection<WorkspaceDetails>> ListWorkspacesAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<WorkspaceDetails>>(
                [new WorkspaceDetails(WorkspaceId, WorkspaceName, DateTimeOffset.UtcNow)]);

        public Task<IReadOnlyCollection<SubjectSummary>> ListSubjectsAsync(Guid workspaceId, CancellationToken ct)
        {
            ListSubjectsCalled = true;
            return Task.FromResult(subjects);
        }

        public Task<SubjectDetails?> GetSubjectAsync(Guid workspaceId, Guid id, CancellationToken ct) =>
            Task.FromResult<SubjectDetails?>(null);

        public Task<SubjectSummary> CreateSubjectAsync(Guid workspaceId, CreateSubjectRequest request, CancellationToken ct) =>
            Task.FromResult(subjects.Single());

        public Task<IReadOnlyCollection<TopicDetails>> ListTopicsAsync(Guid workspaceId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<TopicDetails>>([]);

        public Task<TopicDetails> CreateTopicAsync(Guid workspaceId, Guid subjectId, string name, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<IReadOnlyCollection<StudyNoteDetails>> ListNotesAsync(Guid workspaceId, Guid subjectId, bool includeDescendants, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<StudyNoteDetails>>([]);

        public Task<StudyNoteDetails?> CreateNoteAsync(Guid workspaceId, Guid subjectId, CreateStudyNoteRequest request, CancellationToken ct) =>
            Task.FromResult<StudyNoteDetails?>(null);

        public Task<IReadOnlyCollection<SubjectGoalDetails>> ListGoalsAsync(Guid workspaceId, Guid subjectId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<SubjectGoalDetails>>([]);

        public Task<SubjectGoalDetails?> CreateGoalAsync(Guid workspaceId, Guid subjectId, CreateSubjectGoalRequest request, CancellationToken ct) =>
            Task.FromResult<SubjectGoalDetails?>(null);

        public Task<bool> CompleteGoalAsync(Guid workspaceId, Guid id, CancellationToken ct) => Task.FromResult(false);

        public Task<IReadOnlyCollection<GoalActivityDetails>> ListGoalActivityAsync(Guid workspaceId, DateOnly from, DateOnly to, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<GoalActivityDetails>>([]);
    }
}
