using System.Net.Http.Headers;
using KnowledgeTracker.Mcp.Configuration;

namespace KnowledgeTracker.Mcp.ApplicationApi.Authentication;

public sealed class McpAccessTokenHandler(McpServerOptions options) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        if (options.WorkspaceId is { } workspaceId)
            request.Headers.TryAddWithoutValidation("X-Workspace-Id", workspaceId.ToString());

        return base.SendAsync(request, cancellationToken);
    }
}
