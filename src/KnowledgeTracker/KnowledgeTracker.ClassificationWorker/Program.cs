using System.Data.Common;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.ClassificationWorker;
using KnowledgeTracker.Data.Knowledge.Repositories;
using KnowledgeTracker.Infrastructure.Knowledge.Classification;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

var developmentSettings = FindDevelopmentSettings();
if (developmentSettings is not null)
    builder.Configuration.AddJsonFile(developmentSettings, optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

var connectionString = builder.Configuration.GetConnectionString("KnowledgeTracker")
    ?? builder.Configuration.GetConnectionString("KnowledgeTracker_01")
    ?? throw new InvalidOperationException("A KnowledgeTracker connection string is required.");
var classifierBaseUrl = builder.Configuration["Classification:BaseUrl"] ?? "http://localhost:8021/";

builder.Services.AddSingleton<Func<DbConnection>>(() => new SqlConnection(connectionString));
builder.Services.AddSingleton<IClassificationJobRepository, SqlServerClassificationJobRepository>();
builder.Services.AddSingleton<NoteClassificationProcessor>();
builder.Services.AddHttpClient<INoteClassifier, HttpNoteClassifier>(client =>
{
    client.BaseAddress = new Uri(classifierBaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromMinutes(4);
});
builder.Services.AddHostedService<ClassificationWorker>();

await builder.Build().RunAsync();

static string? FindDevelopmentSettings()
{
    for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
    {
        var candidate = Path.Combine(
            directory.FullName, "src", "KnowledgeTracker", "KnowledgeTracker.Web", "appsettings.Development.json"
        );
        if (File.Exists(candidate))
            return candidate;
    }

    return null;
}
