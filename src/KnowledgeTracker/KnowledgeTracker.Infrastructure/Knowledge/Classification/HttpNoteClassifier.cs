using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KnowledgeTracker.Application.Knowledge;

namespace KnowledgeTracker.Infrastructure.Knowledge.Classification;

public sealed class HttpNoteClassifier(HttpClient httpClient) : INoteClassifier
{
    public async Task<ClassifierResult> ClassifyAsync(
        string text,
        IReadOnlyCollection<ClassificationNode> nodes,
        CancellationToken ct
    )
    {
        var payload = new ClassifyRequest(
            text,
            nodes.Select(node => new ClassificationNodeRequest(
                node.Id, node.Name, node.Description, node.ParentSubjectId
            )).ToArray()
        );
        using var response = await httpClient.PostAsJsonAsync("classify", payload, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ClassifyResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("The classifier returned an empty response.");

        return new ClassifierResult(
            result.Model,
            result.ModelVersion,
            result.Classifications.Select(item => new ClassifierScore(item.NodeId, item.Score)).ToArray()
        );
    }

    private sealed record ClassifyRequest(
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("nodes")] IReadOnlyCollection<ClassificationNodeRequest> Nodes
    );

    private sealed record ClassificationNodeRequest(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("parentId")] Guid? ParentId
    );

    private sealed record ClassifyResponse(
        [property: JsonPropertyName("classifications")] IReadOnlyCollection<ClassificationScoreResponse> Classifications,
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("modelVersion")] string ModelVersion
    );

    private sealed record ClassificationScoreResponse(
        [property: JsonPropertyName("nodeId")] Guid NodeId,
        [property: JsonPropertyName("score")] double Score
    );
}
