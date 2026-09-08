using KnowledgeTracker.Mcp.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace KnowledgeTracker.Tests.Authentication;

public sealed class McpServerConfigurationTests
{
    [Fact]
    public void Load_ReturnsValidatedHttpServerOptions()
    {
        var options = McpServerOptions.Load(BuildConfiguration(
            listenAddress: "127.0.0.1",
            port: "3001",
            endpointPath: "/mcp",
            applicationBaseUrl: "http://localhost:5015",
            accessToken: "mcp_identifier_secret",
            workspaceId: "9c3d8b2e-8f36-4b0f-9d4f-0c91a65d6f7a"));

        Assert.Equal("127.0.0.1", options.ListenAddress);
        Assert.Equal(3001, options.Port);
        Assert.Equal("/mcp", options.McpEndpointPath);
        Assert.Equal("http://localhost:5015/", options.ApplicationBaseUrl);
        Assert.Equal("mcp_identifier_secret", options.AccessToken);
        Assert.Equal(Guid.Parse("9c3d8b2e-8f36-4b0f-9d4f-0c91a65d6f7a"), options.WorkspaceId);
        Assert.Equal("127.0.0.1", options.ListenIpAddress.ToString());
    }

    [Theory]
    [InlineData("ListenAddress", "not-an-ip")]
    [InlineData("Port", "0")]
    [InlineData("Port", "65536")]
    [InlineData("McpEndpointPath", "mcp")]
    [InlineData("McpEndpointPath", "/mcp?query")]
    [InlineData("ApplicationBaseUrl", "not-a-url")]
    [InlineData("AccessToken", "normal-access-token")]
    [InlineData("WorkspaceId", "not-a-guid")]
    public void Load_RejectsInvalidConfiguration(string key, string value)
    {
        var values = CreateValues();
        values[$"McpServer:{key}"] = value;
        var invalidConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            McpServerOptions.Load(invalidConfiguration));

        Assert.Contains(key, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(value, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ListenAddress")]
    [InlineData("McpEndpointPath")]
    [InlineData("ApplicationBaseUrl")]
    [InlineData("AccessToken")]
    public void Load_RejectsMissingRequiredConfiguration(string key)
    {
        var values = CreateValues();
        values[$"McpServer:{key}"] = null;
        var invalidConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            McpServerOptions.Load(invalidConfiguration));

        Assert.Contains(key, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_RejectsMissingSecretWithoutDisclosingConfigurationValues()
    {
        var secret = "mcp_real_identifier_real_secret";
        var configuration = BuildConfiguration(accessToken: " ");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            McpServerOptions.Load(configuration));

        Assert.Contains("AccessToken", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, exception.Message, StringComparison.Ordinal);
    }

    private static IConfiguration BuildConfiguration(
        string? listenAddress = "127.0.0.1",
        string? port = "3001",
        string? endpointPath = "/mcp",
        string? applicationBaseUrl = "http://localhost:5015",
        string? accessToken = "mcp_identifier_secret",
        string? workspaceId = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(CreateValues(listenAddress, port, endpointPath, applicationBaseUrl, accessToken, workspaceId))
            .Build();

    private static Dictionary<string, string?> CreateValues(
        string? listenAddress = "127.0.0.1",
        string? port = "3001",
        string? endpointPath = "/mcp",
        string? applicationBaseUrl = "http://localhost:5015",
        string? accessToken = "mcp_identifier_secret",
        string? workspaceId = null) => new()
    {
        ["McpServer:ListenAddress"] = listenAddress,
        ["McpServer:Port"] = port,
        ["McpServer:McpEndpointPath"] = endpointPath,
        ["McpServer:ApplicationBaseUrl"] = applicationBaseUrl,
        ["McpServer:AccessToken"] = accessToken,
        ["McpServer:WorkspaceId"] = workspaceId,
    };
}
