using System.Net;
using Microsoft.Extensions.Configuration;

namespace KnowledgeTracker.Mcp.Configuration;

public sealed class McpServerOptions
{
    // Runtime settings loaded from the MCP server's local configuration or environment.
    public required string ListenAddress { get; init; }
    public required int Port { get; init; }
    public required string McpEndpointPath { get; init; }
    public required string ApplicationBaseUrl { get; init; }
    public required string AccessToken { get; init; }
    // Empty means every workspace name is allowed; values restrict MCP calls to that set.
    public required IReadOnlyCollection<string> WorkspaceNames { get; init; }
    public required IPAddress ListenIpAddress { get; init; }

    public static McpServerOptions Load(IConfiguration configuration)
    {
        var section = configuration.GetSection("McpServer");
        return ConfigurationValidation.Validate(
            section["ListenAddress"],
            section.GetValue<int?>("Port"),
            section["McpEndpointPath"],
            section["ApplicationBaseUrl"],
            section["AccessToken"],
            section["WorkspaceId"]);
    }
}
