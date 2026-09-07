using System.Net;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Mcp;
using KnowledgeTracker.Mcp.ApplicationApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using Xunit;

namespace KnowledgeTracker.Tests.Mcp;

public sealed class McpHttpIntegrationTests
{
    [Fact]
    public async Task ListSubjects_UsesOfficialStreamableHttpTransportAndRegisteredTool()
    {
        var subjects = new[]
        {
            new SubjectSummary(Guid.NewGuid(), "C#", null, null, null),
        };
        var application = new StubApplicationApiClient(subjects);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
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
        var result = await listSubjects.CallAsync(new Dictionary<string, object?>());

        Assert.NotEqual(true, result.IsError);
        Assert.NotEmpty(result.Content);
        Assert.True(application.ListSubjectsCalled);

        await server.StopAsync();
    }

    private sealed class StubApplicationApiClient(IReadOnlyCollection<SubjectSummary> subjects) : IApplicationApiClient
    {
        public bool ListSubjectsCalled { get; private set; }

        public Task<IReadOnlyCollection<SubjectSummary>> ListSubjectsAsync(CancellationToken ct)
        {
            ListSubjectsCalled = true;
            return Task.FromResult(subjects);
        }

        public Task<SubjectDetails?> GetSubjectAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<SubjectDetails?>(null);

        public Task<SubjectSummary> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken ct) =>
            Task.FromResult(subjects.Single());

        public Task<IReadOnlyCollection<TopicDetails>> ListTopicsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<TopicDetails>>([]);

        public Task<TopicDetails> CreateTopicAsync(Guid subjectId, string name, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<IReadOnlyCollection<StudyNoteDetails>> ListNotesAsync(Guid subjectId, bool includeDescendants, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<StudyNoteDetails>>([]);

        public Task<StudyNoteDetails?> CreateNoteAsync(Guid subjectId, CreateStudyNoteRequest request, CancellationToken ct) =>
            Task.FromResult<StudyNoteDetails?>(null);

        public Task<IReadOnlyCollection<SubjectGoalDetails>> ListGoalsAsync(Guid subjectId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<SubjectGoalDetails>>([]);

        public Task<SubjectGoalDetails?> CreateGoalAsync(Guid subjectId, CreateSubjectGoalRequest request, CancellationToken ct) =>
            Task.FromResult<SubjectGoalDetails?>(null);

        public Task<bool> CompleteGoalAsync(Guid id, CancellationToken ct) => Task.FromResult(false);

        public Task<IReadOnlyCollection<GoalActivityDetails>> ListGoalActivityAsync(DateOnly from, DateOnly to, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<GoalActivityDetails>>([]);
    }
}
