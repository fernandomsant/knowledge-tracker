using System.Net;
using System.Net.Http.Json;
using KnowledgeTracker.Application.Knowledge;

namespace KnowledgeTracker.Mcp.ApplicationApi;

public sealed class ApplicationApiClient(HttpClient httpClient) : IApplicationApiClient
{
    public Task<IReadOnlyCollection<SubjectSummary>> ListSubjectsAsync(CancellationToken ct) =>
        GetAsync<IReadOnlyCollection<SubjectSummary>>("mcp-api/subjects", ct);

    public Task<SubjectDetails?> GetSubjectAsync(Guid id, CancellationToken ct) =>
        GetOptionalAsync<SubjectDetails>($"mcp-api/subjects/{id}", ct);

    public Task<SubjectSummary> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken ct) =>
        PostAsync<CreateSubjectRequest, SubjectSummary>("mcp-api/subjects", request, ct);

    public Task<IReadOnlyCollection<TopicDetails>> ListTopicsAsync(CancellationToken ct) =>
        GetAsync<IReadOnlyCollection<TopicDetails>>("mcp-api/topics", ct);

    public Task<TopicDetails> CreateTopicAsync(Guid subjectId, string name, CancellationToken ct) =>
        PostAsync<CreateTopicRequest, TopicDetails>($"mcp-api/subjects/{subjectId}/topics", new(subjectId, name), ct);

    public Task<IReadOnlyCollection<StudyNoteDetails>> ListNotesAsync(Guid subjectId, bool includeDescendants, CancellationToken ct) =>
        GetAsync<IReadOnlyCollection<StudyNoteDetails>>($"mcp-api/subjects/{subjectId}/notes?includeDescendants={includeDescendants.ToString().ToLowerInvariant()}", ct);

    public Task<StudyNoteDetails?> CreateNoteAsync(Guid subjectId, CreateStudyNoteRequest request, CancellationToken ct) =>
        PostOptionalAsync<CreateStudyNoteRequest, StudyNoteDetails>($"mcp-api/subjects/{subjectId}/notes", request, ct);

    public Task<IReadOnlyCollection<SubjectGoalDetails>> ListGoalsAsync(Guid subjectId, CancellationToken ct) =>
        GetAsync<IReadOnlyCollection<SubjectGoalDetails>>($"mcp-api/subjects/{subjectId}/goals", ct);

    public Task<SubjectGoalDetails?> CreateGoalAsync(Guid subjectId, CreateSubjectGoalRequest request, CancellationToken ct) =>
        PostOptionalAsync<CreateSubjectGoalRequest, SubjectGoalDetails>($"mcp-api/subjects/{subjectId}/goals", request, ct);

    public async Task<bool> CompleteGoalAsync(Guid id, CancellationToken ct)
    {
        using var response = await httpClient.PostAsync($"mcp-api/subject-goals/{id}/complete", content: null, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;
        await EnsureSuccessAsync(response);
        return true;
    }

    public Task<IReadOnlyCollection<GoalActivityDetails>> ListGoalActivityAsync(DateOnly from, DateOnly to, CancellationToken ct) =>
        GetAsync<IReadOnlyCollection<GoalActivityDetails>>($"mcp-api/goal-activity?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct);

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await SendAsync(() => httpClient.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, ct));
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
            ?? throw new ApplicationApiException((int)response.StatusCode, "empty response");
    }

    private async Task<T?> GetOptionalAsync<T>(string path, CancellationToken ct)
    {
        using var response = await SendAsync(() => httpClient.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, ct));
        if (response.StatusCode == HttpStatusCode.NotFound)
            return default;
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken ct)
    {
        using var response = await SendAsync(() => httpClient.PostAsJsonAsync(path, request, cancellationToken: ct));
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct)
            ?? throw new ApplicationApiException((int)response.StatusCode, "empty response");
    }

    private async Task<TResponse?> PostOptionalAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken ct)
    {
        using var response = await SendAsync(() => httpClient.PostAsJsonAsync(path, request, cancellationToken: ct));
        if (response.StatusCode == HttpStatusCode.NotFound)
            return default;
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
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
