using System.Net.Http.Headers;
using KnowledgeTracker.Mcp.Configuration;
using ModelContextProtocol;

namespace KnowledgeTracker.Mcp.ApplicationApi.Authentication;

// Adds the MCP bearer token and carries the workspace ID resolved by KnowledgeTools.
public sealed class McpAccessTokenHandler(McpServerOptions options) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        var workspaceHeaders = request.Headers.TryGetValues("X-Workspace-Id", out var values)
            ? values.ToArray()
            : [];
        if (workspaceHeaders.Length == 0 && IsWorkspaceResolutionRequest(request))
            return base.SendAsync(request, cancellationToken);

        if (workspaceHeaders.Length != 1 || !Guid.TryParse(workspaceHeaders[0], out var workspaceId) || workspaceId == Guid.Empty)
            throw new McpException("A workspace name must be specified for every MCP operation.");

        return base.SendAsync(request, cancellationToken);
    }

    private static bool IsWorkspaceResolutionRequest(HttpRequestMessage request) =>
        request.Method == HttpMethod.Get
        && string.Equals(
            request.RequestUri?.AbsolutePath.Trim('/'),
            "mcp-api/workspaces",
            StringComparison.OrdinalIgnoreCase);
}
