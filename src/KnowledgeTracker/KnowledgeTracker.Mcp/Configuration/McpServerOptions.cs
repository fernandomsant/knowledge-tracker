using System.Net;
using Microsoft.Extensions.Configuration;

namespace KnowledgeTracker.Mcp.Configuration;

public sealed class McpServerOptions
{
    public required string ListenAddress { get; init; }
    public required int Port { get; init; }
    public required string McpEndpointPath { get; init; }
    public required string ApplicationBaseUrl { get; init; }
    public required string AccessToken { get; init; }
    public required IPAddress ListenIpAddress { get; init; }

    public static McpServerOptions Load(IConfiguration configuration)
    {
        var section = configuration.GetSection("McpServer");
        return ConfigurationValidation.Validate(
            section["ListenAddress"],
            section.GetValue<int?>("Port"),
            section["McpEndpointPath"],
            section["ApplicationBaseUrl"],
            section["AccessToken"]);
    }
}
