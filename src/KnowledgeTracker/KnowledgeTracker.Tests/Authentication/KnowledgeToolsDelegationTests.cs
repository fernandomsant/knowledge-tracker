using System.Net;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Domain.Knowledge;
using KnowledgeTracker.Mcp;
using KnowledgeTracker.Mcp.ApplicationApi;
using KnowledgeTracker.Mcp.Configuration;
using ModelContextProtocol;
using Xunit;

namespace KnowledgeTracker.Tests.Authentication;

public sealed class KnowledgeToolsDelegationTests
{
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private const string WorkspaceName = "Personal";

    [Fact]
    public async Task CreateSubjectAsync_DelegatesToDedicatedApplicationRouteClient()
    {
        var client = new RecordingApplicationApiClient();
        var tools = new KnowledgeTools(client, CreateOptions());

        var result = await tools.CreateSubjectAsync(WorkspaceName, "C#", "Language notes", null, CancellationToken.None);

        Assert.Equal(client.CreatedSubject, result);
        Assert.Equal(new CreateSubjectRequest("C#", "Language notes", null), client.CreateSubjectRequest);
        Assert.Equal(WorkspaceId, client.LastWorkspaceId);
    }

    [Fact]
    public async Task CreateNoteAsync_MapsFlatMcpParametersToApplicationRequest()
    {
        var client = new RecordingApplicationApiClient();
        var tools = new KnowledgeTools(client, CreateOptions());
        var subjectId = Guid.NewGuid();
        var topicId = Guid.NewGuid();
        var definitionId = Guid.NewGuid();
        var startedAtUtc = DateTimeOffset.Parse("2026-09-09T12:00:00Z");

        await Assert.ThrowsAsync<KeyNotFoundException>(() => tools.CreateNoteAsync(
            WorkspaceName, subjectId, topicId, "Delegates", "Flat MCP parameters", 45, startedAtUtc,
            [new McpStudyNoteMetric(definitionId, 2.5m)], CancellationToken.None));

        var request = Assert.IsType<CreateStudyNoteRequest>(client.CreateNoteRequest);
        Assert.Equal(topicId, request.TopicId);
        Assert.Equal("Delegates", request.Title);
        Assert.Equal("Flat MCP parameters", request.Content);
        Assert.Equal(TimeSpan.FromMinutes(45), request.StudyDuration);
        Assert.Equal(startedAtUtc, request.StudyStartedAtUtc);
        var metric = Assert.Single(request.Metrics);
        Assert.Equal(new StudyNoteMetricRequest(definitionId, 2.5m), metric);
        Assert.Equal(WorkspaceId, client.LastWorkspaceId);
    }

    [Fact]
    public async Task CreateGoalAsync_MapsFlatMcpParametersToApplicationRequest()
    {
        var client = new RecordingApplicationApiClient();
        var tools = new KnowledgeTools(client, CreateOptions());
        var subjectId = Guid.NewGuid();
        var topicId = Guid.NewGuid();
        var targetDate = new DateOnly(2026, 10, 1);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => tools.CreateGoalAsync(
            WorkspaceName, subjectId, topicId, "Ship it", GoalKind.TargetDate,
            targetDate: targetDate, subGoals: ["Draft", "Review"], cancellationToken: CancellationToken.None));

        var request = Assert.IsType<CreateSubjectGoalRequest>(client.CreateGoalRequest);
        Assert.Equal(topicId, request.TopicId);
        Assert.Equal("Ship it", request.Title);
        Assert.Equal(GoalKind.TargetDate, request.Kind);
        Assert.Null(request.MetricDefinitionId);
        Assert.Null(request.TargetValue);
        Assert.Equal(targetDate, request.TargetDate);
        Assert.Equal(GoalPeriod.AllTime, request.Period);
        Assert.Null(request.PeriodStartDate);
        Assert.Null(request.PeriodEndDate);
        Assert.Equal(["Draft", "Review"], request.SubGoals);
        Assert.Equal(WorkspaceId, client.LastWorkspaceId);
    }

    [Fact]
    public void MutationTools_DoNotExposeApplicationRequestRecordsToMcpClients()
    {
        var parameterTypes = typeof(KnowledgeTools)
            .GetMethods()
            .Where(method => method.Name is nameof(KnowledgeTools.CreateNoteAsync) or nameof(KnowledgeTools.CreateGoalAsync))
            .SelectMany(method => method.GetParameters())
            .Select(parameter => parameter.ParameterType);

        Assert.DoesNotContain(typeof(CreateStudyNoteRequest), parameterTypes);
        Assert.DoesNotContain(typeof(CreateSubjectGoalRequest), parameterTypes);
    }

    [Fact]
    public async Task ListSubjectsAsync_DelegatesToDedicatedApplicationRouteClient()
    {
        var client = new RecordingApplicationApiClient();
        var tools = new KnowledgeTools(client, CreateOptions());

        var result = await tools.ListSubjectsAsync(WorkspaceName, CancellationToken.None);

        Assert.Equal(client.Subjects, result);
        Assert.True(client.ListSubjectsCalled);
        Assert.Equal(WorkspaceId, client.LastWorkspaceId);
    }

    [Theory]
    [InlineData(401, "authentication failure", "MCP application authentication failed.")]
    [InlineData(403, "authorization failure", "The MCP access token is not authorized for this operation.")]
    public async Task ListSubjectsAsync_MapsApplicationAuthFailuresWithoutExposingToken(
        int statusCode,
        string category,
        string expectedMessage)
    {
        var client = new RecordingApplicationApiClient
        {
            Failure = new ApplicationApiException(statusCode, category),
        };
        var tools = new KnowledgeTools(client, CreateOptions());

        var exception = await Assert.ThrowsAsync<McpException>(() =>
            tools.ListSubjectsAsync(WorkspaceName, CancellationToken.None));

        Assert.Equal(expectedMessage, exception.Message);
        Assert.DoesNotContain("mcp_", exception.Message, StringComparison.Ordinal);
    }

    private sealed class RecordingApplicationApiClient : IApplicationApiClient
    {
        public SubjectSummary CreatedSubject { get; } = new(Guid.NewGuid(), "C#", "Language notes", null, null);
        public IReadOnlyCollection<SubjectSummary> Subjects { get; } =
            [new SubjectSummary(Guid.NewGuid(), "C#", null, null, null)];
        public CreateSubjectRequest? CreateSubjectRequest { get; private set; }
        public CreateStudyNoteRequest? CreateNoteRequest { get; private set; }
        public CreateSubjectGoalRequest? CreateGoalRequest { get; private set; }
        public bool ListSubjectsCalled { get; private set; }
        public Guid LastWorkspaceId { get; private set; }
        public Exception? Failure { get; init; }

        public Task<IReadOnlyCollection<WorkspaceDetails>> ListWorkspacesAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<WorkspaceDetails>>(
                [new WorkspaceDetails(WorkspaceId, WorkspaceName, DateTimeOffset.UtcNow)]);

        public Task<IReadOnlyCollection<SubjectSummary>> ListSubjectsAsync(Guid workspaceId, CancellationToken ct)
        {
            ListSubjectsCalled = true;
            LastWorkspaceId = workspaceId;
            if (Failure is not null)
                return Task.FromException<IReadOnlyCollection<SubjectSummary>>(Failure);

            return Task.FromResult(Subjects);
        }

        public Task<SubjectDetails?> GetSubjectAsync(Guid workspaceId, Guid id, CancellationToken ct) =>
            Task.FromResult<SubjectDetails?>(null);

        public Task<SubjectSummary> CreateSubjectAsync(Guid workspaceId, CreateSubjectRequest request, CancellationToken ct)
        {
            CreateSubjectRequest = request;
            LastWorkspaceId = workspaceId;
            return Task.FromResult(CreatedSubject);
        }

        public Task<IReadOnlyCollection<TopicDetails>> ListTopicsAsync(Guid workspaceId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<TopicDetails>>([]);

        public Task<TopicDetails> CreateTopicAsync(Guid workspaceId, Guid subjectId, string name, CancellationToken ct) =>
            Task.FromResult(new TopicDetails(Guid.NewGuid(), subjectId, name));

        public Task<IReadOnlyCollection<StudyNoteDetails>> ListNotesAsync(Guid workspaceId, Guid subjectId, bool includeDescendants, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<StudyNoteDetails>>([]);

        public Task<StudyNoteDetails?> CreateNoteAsync(Guid workspaceId, Guid subjectId, CreateStudyNoteRequest request, CancellationToken ct)
        {
            CreateNoteRequest = request;
            LastWorkspaceId = workspaceId;
            return Task.FromResult<StudyNoteDetails?>(null);
        }

        public Task<IReadOnlyCollection<SubjectGoalDetails>> ListGoalsAsync(Guid workspaceId, Guid subjectId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<SubjectGoalDetails>>([]);

        public Task<SubjectGoalDetails?> CreateGoalAsync(Guid workspaceId, Guid subjectId, CreateSubjectGoalRequest request, CancellationToken ct)
        {
            CreateGoalRequest = request;
            LastWorkspaceId = workspaceId;
            return Task.FromResult<SubjectGoalDetails?>(null);
        }

        public Task<bool> CompleteGoalAsync(Guid workspaceId, Guid id, CancellationToken ct) => Task.FromResult(false);

        public Task<IReadOnlyCollection<GoalActivityDetails>> ListGoalActivityAsync(Guid workspaceId, DateOnly from, DateOnly to, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<GoalActivityDetails>>([]);
    }

    private static McpServerOptions CreateOptions() => new()
    {
        ListenAddress = "127.0.0.1",
        ListenIpAddress = IPAddress.Loopback,
        Port = 3001,
        McpEndpointPath = "/mcp",
        ApplicationBaseUrl = "http://localhost:5015/",
        AccessToken = "mcp_identifier_secret",
        WorkspaceNames = [WorkspaceName],
    };
}
