using KnowledgeTracker.Mcp.ApplicationApi;
using ModelContextProtocol;

namespace KnowledgeTracker.Mcp;

internal static class McpErrorMapper
{
    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (ApplicationApiException exception)
        {
            throw ToMcpException(exception);
        }
    }

    private static McpException ToMcpException(ApplicationApiException exception)
    {
        var message = exception.Category switch
        {
            "authentication failure" => "MCP application authentication failed.",
            "authorization failure" => "The MCP access token is not authorized for this operation.",
            "validation failure" => "The MCP request was invalid.",
            "not found" => "The requested application resource was not found.",
            "conflict" => "The MCP operation conflicts with the current application state.",
            "timeout" => "The application timed out while processing the MCP operation.",
            "unavailable" => "The application is unavailable.",
            "server failure" => "The application failed while processing the MCP operation.",
            _ => "The application rejected the MCP operation."
        };

        return new McpException(message);
    }
}
