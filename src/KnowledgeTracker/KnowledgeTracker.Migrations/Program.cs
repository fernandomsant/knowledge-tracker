using KnowledgeTracker.Migrations;
using System.Text.Json;

var connectionString = MigrationConnectionStringResolver.Resolve(args);
var runner = new MigrationRunner(connectionString);
await runner.RunAsync(CancellationToken.None);

internal static class MigrationConnectionStringResolver
{
    public static string Resolve(string[] args)
    {
        var suppliedConnectionString = GetArgumentValue(args, "--connection-string");
        if (!string.IsNullOrWhiteSpace(suppliedConnectionString))
        {
            return suppliedConnectionString;
        }

        var environmentConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__KnowledgeTracker");

        if (!string.IsNullOrWhiteSpace(environmentConnectionString))
        {
            return environmentConnectionString;
        }

        var developmentConnectionString = LoadDevelopmentConnectionString();
        if (!string.IsNullOrWhiteSpace(developmentConnectionString))
        {
            return developmentConnectionString;
        }

        throw new InvalidOperationException(
            "Set ConnectionStrings__KnowledgeTracker, configure appsettings.Development.json or .env.json, or pass --connection-string <value> to run migrations.");
    }

    private static string? GetArgumentValue(IReadOnlyList<string> args, string argumentName)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], argumentName, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static string? LoadDevelopmentConnectionString()
    {
        var connectionString = LoadConnectionString(FindDevelopmentSettingsPath());
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        return LoadConnectionString(FindEnvironmentSettingsPath());
    }

    private static string? LoadConnectionString(string? settingsPath)
    {
        if (settingsPath is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
        if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings))
        {
            return null;
        }

        return GetConnectionString(connectionStrings, "KnowledgeTracker");
    }

    private static string? FindDevelopmentSettingsPath()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "KnowledgeTracker",
                "KnowledgeTracker.Web",
                "appsettings.Development.json");

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? FindEnvironmentSettingsPath()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var solutionDirectory = File.Exists(Path.Combine(directory.FullName, "KnowledgeTracker.slnx"))
                ? directory
                : new DirectoryInfo(Path.Combine(directory.FullName, "src", "KnowledgeTracker"));
            var candidate = Path.Combine(solutionDirectory.FullName, ".env.json");
            if (File.Exists(Path.Combine(solutionDirectory.FullName, "KnowledgeTracker.slnx")) && File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? GetConnectionString(JsonElement connectionStrings, string name)
    {
        return connectionStrings.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
