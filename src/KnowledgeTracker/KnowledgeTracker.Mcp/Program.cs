using KnowledgeTracker.Mcp.ApplicationApi;
using KnowledgeTracker.Mcp.ApplicationApi.Authentication;
using KnowledgeTracker.Mcp.Configuration;
using KnowledgeTracker.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using McpServerConfiguration = KnowledgeTracker.Mcp.Configuration.McpServerOptions;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Configuration.AddJsonFile(
    "appsettings.mcp.local.json",
    optional: true,
    reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();

var options = McpServerConfiguration.Load(builder.Configuration);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(options.ListenIpAddress, options.Port);
});

builder.Services.AddSingleton(options);
builder.Services.Configure<HostOptions>(hostOptions =>
{
    hostOptions.ShutdownTimeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddTransient<McpAccessTokenHandler>();
builder.Services
    .AddHttpClient<IApplicationApiClient, ApplicationApiClient>(client =>
    {
        client.BaseAddress = new Uri(options.ApplicationBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<McpAccessTokenHandler>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport(transportOptions =>
    {
        transportOptions.SessionMode = HttpServerSessionMode.Stateless;
    })
    .WithTools<KnowledgeTools>();

var app = builder.Build();
app.MapMcp(options.McpEndpointPath);
await app.RunAsync();
