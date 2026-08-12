using System.Text.Json;
using Microsoft.Data.SqlClient;

var connectionString = ConnectionStringResolver.Resolve(args);
await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

try
{
    await StudentWorkspaceSeed.RunAsync(connection, transaction);
    await transaction.CommitAsync();
    Console.WriteLine("Seeded the student workspace.");
}
catch
{
    await transaction.RollbackAsync(CancellationToken.None);
    throw;
}

file static class StudentWorkspaceSeed
{
    public static async Task RunAsync(SqlConnection connection, SqlTransaction transaction)
    {
        await ExecuteAsync(connection, transaction, """
            IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE NormalizedLogin = @normalizedLogin)
            INSERT INTO dbo.Users (Id, Login, NormalizedLogin, PasswordHash)
            VALUES (@id, @login, @normalizedLogin, @passwordHash);
            """,
            ("@id", new Guid("8D11C893-63C2-4C72-93C8-E9329D9A8EE8")),
            ("@login", "student"),
            ("@normalizedLogin", "STUDENT"),
            ("@passwordHash", "600000:c3R1ZGVudC1zZWVkLXYxIQ==:iC42+OEa6bTa4+8NTTMy0qVY668LScq7d8lHVy63YoA="));

        var subjects = new[]
        {
            new SubjectSeed("4DDC929A-5B93-4DB0-BEE0-274F5302EF75", "Computer Science", "Core concepts for software design and problem solving.", null),
            new SubjectSeed("D9674C7B-D6AC-44A2-9531-E22972518F10", "C# Fundamentals", "Language features, types, and object-oriented programming.", "4DDC929A-5B93-4DB0-BEE0-274F5302EF75"),
            new SubjectSeed("7C9C7D9A-A772-4A96-B374-A3B4750FA0F2", "Databases", "Relational modeling and practical SQL queries.", "4DDC929A-5B93-4DB0-BEE0-274F5302EF75"),
            new SubjectSeed("67010711-B6F1-452B-9D6B-21F7FCE8C00D", "Learning Systems", "Practice methods for retaining and applying new knowledge.", null),
        };
        foreach (var subject in subjects)
            await ExecuteAsync(connection, transaction, """
                IF NOT EXISTS (SELECT 1 FROM dbo.Subjects WHERE Id = @id)
                INSERT INTO dbo.Subjects (Id, Name, Description, ParentSubjectId)
                VALUES (@id, @name, @description, @parentSubjectId);
                """,
                ("@id", Guid.Parse(subject.Id)),
                ("@name", subject.Name),
                ("@description", subject.Description),
                ("@parentSubjectId", subject.ParentSubjectId is null ? DBNull.Value : Guid.Parse(subject.ParentSubjectId)));

        var notes = new[]
        {
            new NoteSeed("B3B8371A-5F7D-4D0C-BA4E-BC8FC5D2D406", "D9674C7B-D6AC-44A2-9531-E22972518F10", "Value types and reference types", "Value types store their data directly, while reference types store a reference to an object. Copying each kind has different effects.", 27000000000, "2026-08-04T18:00:00+00:00"),
            new NoteSeed("9C59F504-BFE7-4BA8-A961-312090294BFD", "D9674C7B-D6AC-44A2-9531-E22972518F10", "Composition over inheritance", "Use composition when a type needs capabilities that can vary independently. Inheritance is best reserved for a stable is-a relationship.", 45000000000, "2026-08-06T18:30:00+00:00"),
            new NoteSeed("D1935CF8-F5AD-4A26-AD67-61FAD368BB12", "7C9C7D9A-A772-4A96-B374-A3B4750FA0F2", "Primary keys and foreign keys", "A primary key identifies each row. A foreign key preserves a relationship by referencing a valid row in another table.", 21000000000, "2026-08-07T17:15:00+00:00"),
            new NoteSeed("4839FE22-01C6-48A9-941D-AE21BE89C71D", "67010711-B6F1-452B-9D6B-21F7FCE8C00D", "Active recall session", "Close the notes and explain the idea from memory before checking what was missed. The missed pieces become the next review prompt.", 33000000000, "2026-08-09T14:00:00+00:00"),
        };
        foreach (var note in notes)
            await ExecuteAsync(connection, transaction, """
                IF NOT EXISTS (SELECT 1 FROM dbo.StudyNotes WHERE Id = @id)
                INSERT INTO dbo.StudyNotes (Id, SubjectId, Title, Content, StudyDurationTicks, StudyStartedAtUtc)
                VALUES (@id, @subjectId, @title, @content, @studyDurationTicks, @studyStartedAtUtc);
                """,
                ("@id", Guid.Parse(note.Id)),
                ("@subjectId", Guid.Parse(note.SubjectId)),
                ("@title", note.Title),
                ("@content", note.Content),
                ("@studyDurationTicks", note.StudyDurationTicks),
                ("@studyStartedAtUtc", DateTimeOffset.Parse(note.StudyStartedAtUtc)));

        foreach (var connectionSeed in new[]
        {
            new ConnectionSeed("F7DBD938-11D5-47F1-BC22-3DA692E16BE2", "D9674C7B-D6AC-44A2-9531-E22972518F10", "7C9C7D9A-A772-4A96-B374-A3B4750FA0F2"),
            new ConnectionSeed("75DFF247-855C-42AB-A72F-2EBEFAF62C89", "67010711-B6F1-452B-9D6B-21F7FCE8C00D", "D9674C7B-D6AC-44A2-9531-E22972518F10"),
        })
            await ExecuteAsync(connection, transaction, """
                IF NOT EXISTS (SELECT 1 FROM dbo.SubjectConnections WHERE Id = @id)
                INSERT INTO dbo.SubjectConnections (Id, SubjectId, ConnectedSubjectId)
                VALUES (@id, @subjectId, @connectedSubjectId);
                """,
                ("@id", Guid.Parse(connectionSeed.Id)),
                ("@subjectId", Guid.Parse(connectionSeed.SubjectId)),
                ("@connectedSubjectId", Guid.Parse(connectionSeed.ConnectedSubjectId)));

        foreach (var metric in new[]
        {
            new MetricSeed("B3B8371A-5F7D-4D0C-BA4E-BC8FC5D2D406", "B2B182D0-8709-4328-BDA1-0A73B51D0E82", 18),
            new MetricSeed("9C59F504-BFE7-4BA8-A961-312090294BFD", "6D584D3A-6D8E-4B7A-A9AF-2C52C90DAA5E", 12),
            new MetricSeed("D1935CF8-F5AD-4A26-AD67-61FAD368BB12", "B2B182D0-8709-4328-BDA1-0A73B51D0E82", 14),
        })
            await ExecuteAsync(connection, transaction, """
                IF NOT EXISTS (SELECT 1 FROM dbo.StudyNoteMetrics WHERE StudyNoteId = @studyNoteId AND MetricDefinitionId = @metricDefinitionId)
                INSERT INTO dbo.StudyNoteMetrics (StudyNoteId, MetricDefinitionId, MetricValue)
                VALUES (@studyNoteId, @metricDefinitionId, @metricValue);
                """,
                ("@studyNoteId", Guid.Parse(metric.StudyNoteId)),
                ("@metricDefinitionId", Guid.Parse(metric.MetricDefinitionId)),
                ("@metricValue", metric.Value));
    }

    private static async Task ExecuteAsync(SqlConnection connection, SqlTransaction transaction, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync();
    }

    private sealed record SubjectSeed(string Id, string Name, string Description, string? ParentSubjectId);
    private sealed record NoteSeed(string Id, string SubjectId, string Title, string Content, long StudyDurationTicks, string StudyStartedAtUtc);
    private sealed record ConnectionSeed(string Id, string SubjectId, string ConnectedSubjectId);
    private sealed record MetricSeed(string StudyNoteId, string MetricDefinitionId, decimal Value);
}

file static class ConnectionStringResolver
{
    public static string Resolve(string[] args)
    {
        var argumentValue = GetArgumentValue(args, "--connection-string");
        if (!string.IsNullOrWhiteSpace(argumentValue)) return argumentValue;

        var environmentValue = Environment.GetEnvironmentVariable("ConnectionStrings__KnowledgeTracker")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__KnowledgeTracker_01");
        if (!string.IsNullOrWhiteSpace(environmentValue)) return environmentValue;

        var settingsPath = FindDevelopmentSettingsPath();
        if (settingsPath is not null)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
            if (document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings))
                foreach (var name in new[] { "KnowledgeTracker", "KnowledgeTracker_01" })
                    if (connectionStrings.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
                        return value.GetString()!;
        }

        throw new InvalidOperationException("Set ConnectionStrings__KnowledgeTracker, configure appsettings.Development.json, or pass --connection-string <value> to run the seed.");
    }

    private static string? GetArgumentValue(IReadOnlyList<string> args, string argumentName)
    {
        for (var index = 0; index < args.Count - 1; index++)
            if (string.Equals(args[index], argumentName, StringComparison.OrdinalIgnoreCase)) return args[index + 1];
        return null;
    }

    private static string? FindDevelopmentSettingsPath()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "src", "KnowledgeTracker", "KnowledgeTracker.Web", "appsettings.Development.json");
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }
}
