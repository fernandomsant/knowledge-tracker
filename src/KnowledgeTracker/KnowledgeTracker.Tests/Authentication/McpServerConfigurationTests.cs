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
            accessToken: "mcp_identifier_secret"));

        Assert.Equal("127.0.0.1", options.ListenAddress);
        Assert.Equal(3001, options.Port);
        Assert.Equal("/mcp", options.McpEndpointPath);
        Assert.Equal("http://localhost:5015/", options.ApplicationBaseUrl);
        Assert.Equal("mcp_identifier_secret", options.AccessToken);
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
        string? accessToken = "mcp_identifier_secret") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(CreateValues(listenAddress, port, endpointPath, applicationBaseUrl, accessToken))
            .Build();

    private static Dictionary<string, string?> CreateValues(
        string? listenAddress = "127.0.0.1",
        string? port = "3001",
        string? endpointPath = "/mcp",
        string? applicationBaseUrl = "http://localhost:5015",
        string? accessToken = "mcp_identifier_secret") => new()
    {
        ["McpServer:ListenAddress"] = listenAddress,
        ["McpServer:Port"] = port,
        ["McpServer:McpEndpointPath"] = endpointPath,
        ["McpServer:ApplicationBaseUrl"] = applicationBaseUrl,
        ["McpServer:AccessToken"] = accessToken,
    };
}
