using System.Net;
using System.Net.Http.Json;
using KnowledgeTracker.Application.Knowledge;

namespace KnowledgeTracker.Mcp.ApplicationApi;

// Authenticated adapter from MCP operations to dedicated Web API routes.
public sealed class ApplicationApiClient(HttpClient httpClient) : IApplicationApiClient
{
    public Task<IReadOnlyCollection<WorkspaceDetails>> ListWorkspacesAsync(CancellationToken ct) =>
        GetAsync<IReadOnlyCollection<WorkspaceDetails>>("mcp-api/workspaces", ct);

    public Task<IReadOnlyCollection<SubjectSummary>> ListSubjectsAsync(Guid workspaceId, CancellationToken ct) =>
        GetAsync<IReadOnlyCollection<SubjectSummary>>("mcp-api/subjects", workspaceId, ct);

    public Task<SubjectDetails?> GetSubjectAsync(Guid workspaceId, Guid id, CancellationToken ct) =>
        GetOptionalAsync<SubjectDetails>($"mcp-api/subjects/{id}", workspaceId, ct);

    public Task<SubjectSummary> CreateSubjectAsync(Guid workspaceId, CreateSubjectRequest request, CancellationToken ct) =>
        PostAsync<CreateSubjectRequest, SubjectSummary>("mcp-api/subjects", workspaceId, request, ct);

    public Task<IReadOnlyCollection<TopicDetails>> ListTopicsAsync(Guid workspaceId, CancellationToken ct) =>
        GetAsync<IReadOnlyCollection<TopicDetails>>("mcp-api/topics", workspaceId, ct);

    public Task<TopicDetails> CreateTopicAsync(Guid workspaceId, Guid subjectId, string name, CancellationToken ct) =>
        PostAsync<CreateTopicRequest, TopicDetails>($"mcp-api/subjects/{subjectId}/topics", workspaceId, new(subjectId, name), ct);

    public Task<IReadOnlyCollection<StudyNoteDetails>> ListNotesAsync(Guid workspaceId, Guid subjectId, bool includeDescendants, CancellationToken ct) =>
        GetAsync<IReadOnlyCollection<StudyNoteDetails>>($"mcp-api/subjects/{subjectId}/notes?includeDescendants={includeDescendants.ToString().ToLowerInvariant()}", workspaceId, ct);

    public Task<StudyNoteDetails?> CreateNoteAsync(Guid workspaceId, Guid subjectId, CreateStudyNoteRequest request, CancellationToken ct) =>
        PostOptionalAsync<CreateStudyNoteRequest, StudyNoteDetails>($"mcp-api/subjects/{subjectId}/notes", workspaceId, request, ct);

    public Task<IReadOnlyCollection<SubjectGoalDetails>> ListGoalsAsync(Guid workspaceId, Guid subjectId, CancellationToken ct) =>
        GetAsync<IReadOnlyCollection<SubjectGoalDetails>>($"mcp-api/subjects/{subjectId}/goals", workspaceId, ct);

    public Task<SubjectGoalDetails?> CreateGoalAsync(Guid workspaceId, Guid subjectId, CreateSubjectGoalRequest request, CancellationToken ct) =>
        PostOptionalAsync<CreateSubjectGoalRequest, SubjectGoalDetails>($"mcp-api/subjects/{subjectId}/goals", workspaceId, request, ct);

    public async Task<bool> CompleteGoalAsync(Guid workspaceId, Guid id, CancellationToken ct)
    {
        using var request = CreateRequest(HttpMethod.Post, $"mcp-api/subject-goals/{id}/complete", workspaceId);
        using var response = await SendAsync(() => httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct));
        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;
        await EnsureSuccessAsync(response);
        return true;
    }

    public Task<IReadOnlyCollection<GoalActivityDetails>> ListGoalActivityAsync(Guid workspaceId, DateOnly from, DateOnly to, CancellationToken ct) =>
        GetAsync<IReadOnlyCollection<GoalActivityDetails>>($"mcp-api/goal-activity?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", workspaceId, ct);

    private Task<T> GetAsync<T>(string path, Guid workspaceId, CancellationToken ct) =>
        GetAsync<T>(path, ct, workspaceId);

    private async Task<T> GetAsync<T>(string path, CancellationToken ct, Guid? workspaceId = null)
    {
        using var request = CreateRequest(HttpMethod.Get, path, workspaceId);
        using var response = await SendAsync(() => httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct));
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
            ?? throw new ApplicationApiException((int)response.StatusCode, "empty response");
    }

    private async Task<T?> GetOptionalAsync<T>(string path, Guid workspaceId, CancellationToken ct)
    {
        using var request = CreateRequest(HttpMethod.Get, path, workspaceId);
        using var response = await SendAsync(() => httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct));
        if (response.StatusCode == HttpStatusCode.NotFound)
            return default;
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, Guid workspaceId, TRequest body, CancellationToken ct)
    {
        using var request = CreateRequest(HttpMethod.Post, path, workspaceId);
        request.Content = JsonContent.Create(body);
        using var response = await SendAsync(() => httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct));
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct)
            ?? throw new ApplicationApiException((int)response.StatusCode, "empty response");
    }

    private async Task<TResponse?> PostOptionalAsync<TRequest, TResponse>(string path, Guid workspaceId, TRequest body, CancellationToken ct)
    {
        using var request = CreateRequest(HttpMethod.Post, path, workspaceId);
        request.Content = JsonContent.Create(body);
        using var response = await SendAsync(() => httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct));
        if (response.StatusCode == HttpStatusCode.NotFound)
            return default;
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path, Guid? workspaceId)
    {
        var request = new HttpRequestMessage(method, path);
        if (workspaceId is { } id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("A workspace identifier is required.", nameof(workspaceId));

            request.Headers.TryAddWithoutValidation("X-Workspace-Id", id.ToString());
        }
        return request;
    }

    private static async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            return await send();
        }
        catch (TaskCanceledException exception) when (!exception.CancellationToken.IsCancellationRequested)
        {
            throw new ApplicationApiException(0, "timeout");
        }
        catch (HttpRequestException)
        {
            throw new ApplicationApiException(0, "unavailable");
        }
    }

    private static Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return Task.CompletedTask;

        var category = (int)response.StatusCode switch
        {
            400 => "validation failure",
            401 => "authentication failure",
            403 => "authorization failure",
            404 => "not found",
            409 => "conflict",
            _ when (int)response.StatusCode >= 500 => "server failure",
            _ => "request failure"
        };
        throw new ApplicationApiException((int)response.StatusCode, category);
    }
}
