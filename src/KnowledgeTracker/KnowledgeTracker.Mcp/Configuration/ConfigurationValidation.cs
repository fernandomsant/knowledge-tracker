using System.Net;

namespace KnowledgeTracker.Mcp.Configuration;

internal static class ConfigurationValidation
{
    public static McpServerOptions Validate(
        string? listenAddress,
        int? port,
        string? mcpEndpointPath,
        string? applicationBaseUrl,
        string? accessToken,
        string? workspaceId)
    {
        var normalizedListenAddress = listenAddress?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedListenAddress)
            || !IPAddress.TryParse(normalizedListenAddress, out var listenIpAddress))
            throw new InvalidOperationException(
                "MCP server configuration value 'McpServer:ListenAddress' must be a valid IP address.");

        if (port is null or <= IPEndPoint.MinPort or > IPEndPoint.MaxPort)
            throw new InvalidOperationException(
                "MCP server configuration value 'McpServer:Port' must be between 1 and 65535.");

        var normalizedEndpointPath = mcpEndpointPath?.Trim();
        if (!IsValidEndpointPath(normalizedEndpointPath))
            throw new InvalidOperationException(
                "MCP server configuration value 'McpServer:McpEndpointPath' must be an absolute path without a query or fragment.");

        var normalizedApplicationBaseUrl = applicationBaseUrl?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedApplicationBaseUrl))
            throw new InvalidOperationException(
                "MCP server configuration value 'McpServer:ApplicationBaseUrl' is required.");
        if (!Uri.TryCreate(normalizedApplicationBaseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException(
                "MCP server configuration value 'McpServer:ApplicationBaseUrl' must be an absolute HTTP or HTTPS URL.");

        var normalizedAccessToken = accessToken?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedAccessToken))
            throw new InvalidOperationException(
                "MCP server configuration value 'McpServer:AccessToken' is required.");
        if (!normalizedAccessToken.StartsWith("mcp_", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "MCP server configuration value 'McpServer:AccessToken' must be an MCP access token.");

        var normalizedWorkspaceId = workspaceId?.Trim();
        Guid? parsedWorkspaceId = null;
        if (!string.IsNullOrWhiteSpace(normalizedWorkspaceId))
        {
            if (!Guid.TryParse(normalizedWorkspaceId, out var workspaceGuid) || workspaceGuid == Guid.Empty)
                throw new InvalidOperationException(
                    "MCP server configuration value 'McpServer:WorkspaceId' must be a valid workspace identifier.");

            parsedWorkspaceId = workspaceGuid;
        }

        return new McpServerOptions
        {
            ListenAddress = normalizedListenAddress,
            ListenIpAddress = listenIpAddress,
            Port = port.Value,
            McpEndpointPath = normalizedEndpointPath!,
            ApplicationBaseUrl = baseUri.ToString().TrimEnd('/') + "/",
            AccessToken = normalizedAccessToken,
            WorkspaceId = parsedWorkspaceId,
        };
    }

    private static bool IsValidEndpointPath(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && path.StartsWith("/", StringComparison.Ordinal)
        && !path.StartsWith("//", StringComparison.Ordinal)
        && !path.Contains('?', StringComparison.Ordinal)
        && !path.Contains('#', StringComparison.Ordinal)
        && !path.Any(char.IsWhiteSpace);
}
