using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Mcp;
using KnowledgeTracker.Mcp.ApplicationApi;
using ModelContextProtocol;
using Xunit;

namespace KnowledgeTracker.Tests.Authentication;

public sealed class KnowledgeToolsDelegationTests
{
    [Fact]
    public async Task CreateSubjectAsync_DelegatesToDedicatedApplicationRouteClient()
    {
        var client = new RecordingApplicationApiClient();
        var tools = new KnowledgeTools(client);

        var result = await tools.CreateSubjectAsync("C#", "Language notes", null, CancellationToken.None);

        Assert.Equal(client.CreatedSubject, result);
        Assert.Equal(new CreateSubjectRequest("C#", "Language notes", null), client.CreateSubjectRequest);
    }

    [Fact]
    public async Task ListSubjectsAsync_DelegatesToDedicatedApplicationRouteClient()
    {
        var client = new RecordingApplicationApiClient();
        var tools = new KnowledgeTools(client);

        var result = await tools.ListSubjectsAsync(CancellationToken.None);

        Assert.Equal(client.Subjects, result);
        Assert.True(client.ListSubjectsCalled);
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
        var tools = new KnowledgeTools(client);

        var exception = await Assert.ThrowsAsync<McpException>(() =>
            tools.ListSubjectsAsync(CancellationToken.None));

        Assert.Equal(expectedMessage, exception.Message);
        Assert.DoesNotContain("mcp_", exception.Message, StringComparison.Ordinal);
    }

    private sealed class RecordingApplicationApiClient : IApplicationApiClient
    {
        public SubjectSummary CreatedSubject { get; } = new(Guid.NewGuid(), "C#", "Language notes", null, null);
        public IReadOnlyCollection<SubjectSummary> Subjects { get; } =
            [new SubjectSummary(Guid.NewGuid(), "C#", null, null, null)];
        public CreateSubjectRequest? CreateSubjectRequest { get; private set; }
        public bool ListSubjectsCalled { get; private set; }
        public Exception? Failure { get; init; }

        public Task<IReadOnlyCollection<SubjectSummary>> ListSubjectsAsync(CancellationToken ct)
        {
            ListSubjectsCalled = true;
            if (Failure is not null)
                return Task.FromException<IReadOnlyCollection<SubjectSummary>>(Failure);

            return Task.FromResult(Subjects);
        }

        public Task<SubjectDetails?> GetSubjectAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<SubjectDetails?>(null);

        public Task<SubjectSummary> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken ct)
        {
            CreateSubjectRequest = request;
            return Task.FromResult(CreatedSubject);
        }

        public Task<IReadOnlyCollection<TopicDetails>> ListTopicsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<TopicDetails>>([]);

        public Task<TopicDetails> CreateTopicAsync(Guid subjectId, string name, CancellationToken ct) =>
            Task.FromResult(new TopicDetails(Guid.NewGuid(), subjectId, name));

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
