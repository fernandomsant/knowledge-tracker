namespace KnowledgeTracker.Mcp.ApplicationApi;

public sealed class ApplicationApiException(int statusCode, string category)
    : Exception($"The application API returned {category} ({statusCode}).")
{
    public int StatusCode { get; } = statusCode;
    public string Category { get; } = category;
}
