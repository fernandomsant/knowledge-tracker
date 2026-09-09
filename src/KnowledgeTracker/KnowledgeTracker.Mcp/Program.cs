using KnowledgeTracker.Mcp.ApplicationApi;
using KnowledgeTracker.Mcp.ApplicationApi.Authentication;
using KnowledgeTracker.Mcp.Configuration;
using KnowledgeTracker.Mcp;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using McpServerConfiguration = KnowledgeTracker.Mcp.Configuration.McpServerOptions;

var environmentSettings = LoadEnvironmentSettings();
var builder = WebApplication.CreateBuilder(args);
if (environmentSettings is not null)
{
    builder.Configuration.AddInMemoryCollection(environmentSettings
        .Where(setting => !string.IsNullOrWhiteSpace(setting.Value) && string.IsNullOrWhiteSpace(builder.Configuration[setting.Key]))
        .ToDictionary(setting => setting.Key, setting => setting.Value));
}
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

static Dictionary<string, string?>? LoadEnvironmentSettings()
{
    for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
    {
        var solutionDirectory = File.Exists(Path.Combine(directory.FullName, "KnowledgeTracker.slnx"))
            ? directory
            : new DirectoryInfo(Path.Combine(directory.FullName, "src", "KnowledgeTracker"));
        var candidate = Path.Combine(solutionDirectory.FullName, ".env.json");
        if (File.Exists(Path.Combine(solutionDirectory.FullName, "KnowledgeTracker.slnx")) && File.Exists(candidate))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(candidate));
            var settings = new Dictionary<string, string?>();
            AddConfigurationValues(document.RootElement, null, settings);
            return settings;
        }
    }

    return null;
}

static void AddConfigurationValues(JsonElement element, string? prefix, Dictionary<string, string?> settings)
{
    foreach (var property in element.EnumerateObject())
    {
        var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";
        if (property.Value.ValueKind == JsonValueKind.Object)
            AddConfigurationValues(property.Value, key, settings);
        else if (property.Value.ValueKind != JsonValueKind.Null)
            settings[key] = property.Value.ToString();
    }
}
