using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Mcp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using Xunit;

namespace KnowledgeTracker.Tests.Mcp;

public sealed class McpProcessHttpIntegrationTests
{
    private const string TestToken = "mcp_phase4_identifier_secret";

    [Fact]
    public async Task IndependentProcess_RemainsAvailableAcrossClientsAndReleasesConfiguredListenerOnStop()
    {
        var fakeApplication = await StartFakeApplicationAsync();
        await using var fakeApplicationServer = fakeApplication.Server;

        var mcpPort = GetFreePort();
        using var process = StartMcpProcess(
            mcpPort,
            fakeApplication.BaseAddress);

        try
        {
            await WaitForPortAsync(mcpPort);

            await using (var firstClient = await ConnectAsync(mcpPort))
            {
                var tools = await firstClient.ListToolsAsync();
                Assert.Equal(
                    [
                        "complete_goal", "create_goal", "create_note", "create_subject",
                        "create_topic", "get_subject", "list_goal_activity", "list_goals",
                        "list_notes", "list_subjects", "list_topics",
                    ],
                    tools.Select(tool => tool.Name).OrderBy(name => name));

                var listSubjects = tools.Single(tool => tool.Name == "list_subjects");
                var result = await listSubjects.CallAsync(new Dictionary<string, object?>());

                Assert.NotEqual(true, result.IsError);
                Assert.True(fakeApplication.State.ValidMcpAuthorization);
                Assert.False(JsonSerializer.Serialize(result).Contains(TestToken, StringComparison.Ordinal));
            }

            Assert.False(process.HasExited);

            await using (var secondClient = await ConnectAsync(mcpPort))
            {
                var tools = await secondClient.ListToolsAsync();
                Assert.Contains(tools, tool => tool.Name == "list_subjects");
            }

            Assert.False(process.HasExited);
        }
        finally
        {
            await StopProcessAsync(process);
        }

        Assert.True(await WaitForPortReleaseAsync(mcpPort));
    }

    [Fact]
    public async Task HttpHost_FailsWhenConfiguredPortIsAlreadyBound()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port));
        await using var server = builder.Build();

        await Assert.ThrowsAnyAsync<Exception>(() => server.StartAsync());
    }

    private static async Task<(WebApplication Server, FakeApplicationState State, Uri BaseAddress)> StartFakeApplicationAsync()
    {
        var state = new FakeApplicationState();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));

        var server = builder.Build();
        server.MapGet("/mcp-api/subjects", (HttpRequest request) =>
        {
            state.ValidMcpAuthorization = request.Headers.Authorization == $"Bearer {TestToken}";
            return Results.Json(new[]
            {
                new SubjectSummary(Guid.NewGuid(), "Phase 4", null, null, null),
            });
        });
        await server.StartAsync();

        var address = server.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()!
            .Addresses
            .Single();

        return (server, state, new Uri(address));
    }

    private static Process StartMcpProcess(int port, Uri applicationBaseAddress)
    {
        var assemblyPath = typeof(KnowledgeTools).Assembly.Location;
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = Path.GetDirectoryName(assemblyPath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add(assemblyPath);
        startInfo.Environment["McpServer__ListenAddress"] = "127.0.0.1";
        startInfo.Environment["McpServer__Port"] = port.ToString();
        startInfo.Environment["McpServer__McpEndpointPath"] = "/mcp";
        startInfo.Environment["McpServer__ApplicationBaseUrl"] = applicationBaseAddress.ToString();
        startInfo.Environment["McpServer__AccessToken"] = TestToken;

        var process = Process.Start(startInfo)!;
        _ = process.StandardOutput.ReadToEndAsync();
        _ = process.StandardError.ReadToEndAsync();
        return process;
    }

    private static async Task<McpClient> ConnectAsync(int port) =>
        await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri($"http://127.0.0.1:{port}/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
        }));

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static async Task WaitForPortAsync(int port)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!timeout.IsCancellationRequested)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(IPAddress.Loopback, port, timeout.Token);
                return;
            }
            catch (SocketException)
            {
                await Task.Delay(50, timeout.Token);
            }
        }

        throw new TimeoutException("The MCP process did not open its configured port.");
    }

    private static async Task<bool> WaitForPortReleaseAsync(int port)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!timeout.IsCancellationRequested)
        {
            try
            {
                using var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                return true;
            }
            catch (SocketException)
            {
                await Task.Delay(50, timeout.Token);
            }
        }

        return false;
    }

    private static async Task StopProcessAsync(Process process)
    {
        if (!process.HasExited)
            process.Kill(entireProcessTree: true);

        await process.WaitForExitAsync();
    }

    private sealed class FakeApplicationState
    {
        public bool ValidMcpAuthorization { get; set; }
    }
}
