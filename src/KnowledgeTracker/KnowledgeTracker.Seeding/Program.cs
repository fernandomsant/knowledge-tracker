using System.Security.Cryptography;
using System.Text;
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
            new SubjectSeed(SeedId("subject-algorithms"), "Algorithms", "Problem solving, complexity, and core data structures.", "4DDC929A-5B93-4DB0-BEE0-274F5302EF75"),
            new SubjectSeed(SeedId("subject-sql-server"), "SQL Server", "Query design, indexing, and SQL Server operations.", "7C9C7D9A-A772-4A96-B374-A3B4750FA0F2"),
            new SubjectSeed(SeedId("subject-data-modeling"), "Data Modeling", "Entities, relationships, normalization, and constraints.", "7C9C7D9A-A772-4A96-B374-A3B4750FA0F2"),
            new SubjectSeed(SeedId("subject-networking"), "Networking", "Protocols, addressing, and reliable communication.", null),
            new SubjectSeed(SeedId("subject-mathematics"), "Mathematics", "Mathematical tools for computing and analysis.", null),
            new SubjectSeed(SeedId("subject-linear-algebra"), "Linear Algebra", "Vectors, matrices, and transformations.", SeedId("subject-mathematics")),
            new SubjectSeed(SeedId("subject-german"), "German", "Vocabulary, grammar, and listening practice.", null),
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

        foreach (var topic in subjects.Select(subject => new TopicSeed(subject.Id, subject.Name)).Concat([
            new TopicSeed(SeedId("topic-linux"), "Linux"),
            new TopicSeed(SeedId("topic-network-security"), "Network Security"),
            new TopicSeed(SeedId("topic-german-grammar"), "German Grammar"),
            new TopicSeed(SeedId("topic-data-structures"), "Data Structures")
        ]))
            await ExecuteAsync(connection, transaction, """
                IF NOT EXISTS (SELECT 1 FROM dbo.Topics WHERE Id = @id)
                INSERT INTO dbo.Topics (Id, Name) VALUES (@id, @name);
                """, ("@id", Guid.Parse(topic.Id)), ("@name", topic.Name));

        var notes = new[]
        {
            new NoteSeed("B3B8371A-5F7D-4D0C-BA4E-BC8FC5D2D406", "D9674C7B-D6AC-44A2-9531-E22972518F10", "Value types and reference types", "Value types store their data directly, while reference types store a reference to an object. Copying each kind has different effects.", 27000000000, "2026-08-04T18:00:00+00:00"),
            new NoteSeed("9C59F504-BFE7-4BA8-A961-312090294BFD", "D9674C7B-D6AC-44A2-9531-E22972518F10", "Composition over inheritance", "Use composition when a type needs capabilities that can vary independently. Inheritance is best reserved for a stable is-a relationship.", 45000000000, "2026-08-06T18:30:00+00:00"),
            new NoteSeed("D1935CF8-F5AD-4A26-AD67-61FAD368BB12", "7C9C7D9A-A772-4A96-B374-A3B4750FA0F2", "Primary keys and foreign keys", "A primary key identifies each row. A foreign key preserves a relationship by referencing a valid row in another table.", 21000000000, "2026-08-07T17:15:00+00:00"),
            new NoteSeed("4839FE22-01C6-48A9-941D-AE21BE89C71D", "67010711-B6F1-452B-9D6B-21F7FCE8C00D", "Active recall session", "Close the notes and explain the idea from memory before checking what was missed. The missed pieces become the next review prompt.", 33000000000, "2026-08-09T14:00:00+00:00"),
            Note("csharp-interfaces", "D9674C7B-D6AC-44A2-9531-E22972518F10", "Interfaces define contracts", "Interfaces describe the behavior a type promises to provide. They keep callers independent from a concrete implementation.", 42, "2026-06-18T18:00:00+00:00"),
            Note("csharp-async", "D9674C7B-D6AC-44A2-9531-E22972518F10", "Async work and cancellation", "Async methods should expose cancellation where work can be abandoned and avoid blocking a thread while I/O is pending.", 55, "2026-06-25T18:15:00+00:00"),
            Note("csharp-linq", "D9674C7B-D6AC-44A2-9531-E22972518F10", "LINQ query boundaries", "Keep queryable database expressions separate from in-memory transformations so expensive work remains visible.", 48, "2026-07-03T17:45:00+00:00"),
            Note("csharp-testing", "D9674C7B-D6AC-44A2-9531-E22972518F10", "Arrange act assert", "A focused test sets up one scenario, exercises one behavior, and verifies the observable result.", 35, "2026-07-21T18:30:00+00:00"),
            Note("database-normalization", "7C9C7D9A-A772-4A96-B374-A3B4750FA0F2", "Why normalize data", "Normalization reduces duplicated facts and prevents update anomalies by separating independent concepts.", 50, "2026-06-20T17:30:00+00:00"),
            Note("database-transactions", "7C9C7D9A-A772-4A96-B374-A3B4750FA0F2", "Transactions protect invariants", "A transaction groups related writes so a failure cannot leave a business operation half complete.", 46, "2026-07-01T17:30:00+00:00"),
            Note("database-isolation", "7C9C7D9A-A772-4A96-B374-A3B4750FA0F2", "Isolation levels trade consistency and concurrency", "The selected isolation level determines which concurrent changes a query can observe.", 44, "2026-07-29T18:00:00+00:00"),
            Note("algorithms-big-o", SeedId("subject-algorithms"), "Big O describes growth", "Complexity compares how resource use grows as input size increases, not the exact runtime of one machine.", 40, "2026-06-16T16:30:00+00:00"),
            Note("algorithms-binary-search", SeedId("subject-algorithms"), "Binary search needs sorted input", "Each comparison discards half of a sorted search space, producing logarithmic lookup time.", 36, "2026-06-30T16:00:00+00:00"),
            Note("algorithms-graphs", SeedId("subject-algorithms"), "Breadth-first search explores by distance", "A queue lets breadth-first search visit every node one edge farther away before moving deeper.", 58, "2026-07-16T16:00:00+00:00"),
            Note("sql-indexes", SeedId("subject-sql-server"), "Indexes accelerate selective lookups", "An index is most valuable when it avoids reading a large portion of the table and matches the query predicate.", 52, "2026-06-23T19:00:00+00:00"),
            Note("sql-query-plans", SeedId("subject-sql-server"), "Read execution plans as evidence", "An execution plan shows the operators selected by the optimizer and helps locate high-cost scans and joins.", 47, "2026-07-08T19:00:00+00:00"),
            Note("sql-window-functions", SeedId("subject-sql-server"), "Window functions keep row detail", "Window functions calculate aggregates across a related set while preserving the individual rows in the result.", 41, "2026-08-02T19:00:00+00:00"),
            Note("model-relationships", SeedId("subject-data-modeling"), "Model cardinality explicitly", "A relationship should state whether each side is optional and how many related records are valid.", 45, "2026-06-27T17:00:00+00:00"),
            Note("model-constraints", SeedId("subject-data-modeling"), "Constraints are executable rules", "Database constraints keep invalid states out even when several applications write to the same data.", 38, "2026-07-11T17:00:00+00:00"),
            Note("networking-tcp", SeedId("subject-networking"), "TCP provides ordered delivery", "TCP establishes a connection and uses acknowledgements and retransmission to provide ordered reliable delivery.", 49, "2026-06-19T15:30:00+00:00"),
            Note("networking-dns", SeedId("subject-networking"), "DNS resolves names through delegation", "Resolvers follow referrals through the hierarchy until an authoritative answer is found or cached.", 33, "2026-07-05T15:30:00+00:00"),
            Note("networking-http", SeedId("subject-networking"), "HTTP methods communicate intent", "GET retrieves representations, POST submits work, and idempotent methods can be safely retried.", 43, "2026-07-24T15:30:00+00:00"),
            Note("math-vectors", SeedId("subject-linear-algebra"), "Vectors represent direction and magnitude", "A vector can be scaled, added, and projected to describe geometric and computational relationships.", 39, "2026-06-22T14:00:00+00:00"),
            Note("math-matrices", SeedId("subject-linear-algebra"), "Matrices represent transformations", "Matrix multiplication composes linear transformations and the order of multiplication matters.", 51, "2026-07-13T14:00:00+00:00"),
            Note("german-greetings", SeedId("subject-german"), "German greetings vary by formality", "Guten Morgen and Guten Tag work in formal contexts, while Hallo is a flexible informal greeting.", 25, "2026-06-17T20:00:00+00:00"),
            Note("german-cases", SeedId("subject-german"), "Articles signal grammatical case", "German articles change with gender, number, and case, so learning noun phrases together is useful.", 37, "2026-07-06T20:00:00+00:00"),
            Note("german-listening", SeedId("subject-german"), "Listen for familiar chunks", "Short repeated listening sessions help identify frequent phrases before every word is understood.", 30, "2026-08-05T20:00:00+00:00"),
            Note("learning-spaced-repetition", "67010711-B6F1-452B-9D6B-21F7FCE8C00D", "Spacing creates useful difficulty", "Reviewing after partial forgetting requires retrieval effort and strengthens long-term memory more than immediate rereading.", 32, "2026-06-21T13:00:00+00:00"),
            Note("learning-interleaving", "67010711-B6F1-452B-9D6B-21F7FCE8C00D", "Interleaving improves discrimination", "Mixing related problem types requires deciding which method applies instead of repeating one routine.", 34, "2026-07-18T13:00:00+00:00"),
            Note("learning-review-plan", "67010711-B6F1-452B-9D6B-21F7FCE8C00D", "Build a weekly review plan", "Schedule a short review block for each active subject and adjust it using missed retrieval prompts.", 29, "2026-08-10T13:00:00+00:00"),
            Note("csharp-generics", "D9674C7B-D6AC-44A2-9531-E22972518F10", "Generic constraints communicate intent", "Constraints describe the capabilities a type parameter needs and keep invalid generic combinations out at compile time.", 46, "2026-08-11T18:00:00+00:00"),
            Note("csharp-patterns", "D9674C7B-D6AC-44A2-9531-E22972518F10", "Pattern matching narrows values", "Patterns combine a type test and extraction into one readable branch while preserving compiler flow analysis.", 43, "2026-08-12T18:15:00+00:00"),
            Note("database-index-maintenance", SeedId("subject-sql-server"), "Indexes need measured maintenance", "Rebuild or reorganize decisions should be based on workload and fragmentation evidence rather than a fixed schedule.", 41, "2026-08-11T19:00:00+00:00"),
            Note("algorithms-dynamic-programming", SeedId("subject-algorithms"), "Dynamic programming reuses subproblems", "Store the result of overlapping subproblems when the optimal solution can be built from smaller optimal solutions.", 57, "2026-08-12T16:00:00+00:00"),
            Note("networking-tls", SeedId("subject-networking"), "TLS authenticates and encrypts", "A TLS handshake agrees keys and authenticates the endpoint before protected application data is exchanged.", 45, "2026-08-11T15:30:00+00:00"),
            Note("math-eigenvectors", SeedId("subject-linear-algebra"), "Eigenvectors preserve direction", "An eigenvector keeps its direction under a linear transformation while the eigenvalue describes its scaling.", 48, "2026-08-12T14:00:00+00:00"),
            Note("german-modal-verbs", SeedId("subject-german"), "Modal verbs move the infinitive", "In main clauses a modal verb occupies the finite verb position while the main infinitive moves to the end.", 34, "2026-08-11T20:00:00+00:00"),
            Note("learning-feedback", "67010711-B6F1-452B-9D6B-21F7FCE8C00D", "Feedback should follow retrieval", "Attempting recall before checking an answer makes corrective feedback more diagnostic and memorable.", 31, "2026-08-12T13:00:00+00:00"),
        };
        foreach (var note in notes)
            await ExecuteAsync(connection, transaction, """
                IF NOT EXISTS (SELECT 1 FROM dbo.StudyNotes WHERE Id = @id)
                INSERT INTO dbo.StudyNotes (Id, SubjectId, TopicId, Title, Content, StudyDurationTicks, StudyStartedAtUtc)
                VALUES (@id, @subjectId, @subjectId, @title, @content, @studyDurationTicks, @studyStartedAtUtc);
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
            new ConnectionSeed(SeedId("connection-algorithms-csharp"), SeedId("subject-algorithms"), "D9674C7B-D6AC-44A2-9531-E22972518F10"),
            new ConnectionSeed(SeedId("connection-sql-modeling"), SeedId("subject-sql-server"), SeedId("subject-data-modeling")),
            new ConnectionSeed(SeedId("connection-networking-databases"), SeedId("subject-networking"), "7C9C7D9A-A772-4A96-B374-A3B4750FA0F2"),
            new ConnectionSeed(SeedId("connection-german-learning"), SeedId("subject-german"), "67010711-B6F1-452B-9D6B-21F7FCE8C00D"),
        })
            await ExecuteAsync(connection, transaction, """
                IF NOT EXISTS (SELECT 1 FROM dbo.SubjectConnections WHERE Id = @id)
                INSERT INTO dbo.SubjectConnections (Id, SubjectId, ConnectedSubjectId)
                VALUES (@id, @subjectId, @connectedSubjectId);
                """,
                ("@id", Guid.Parse(connectionSeed.Id)),
                ("@subjectId", Guid.Parse(connectionSeed.SubjectId)),
                ("@connectedSubjectId", Guid.Parse(connectionSeed.ConnectedSubjectId)));

        var metrics = new List<MetricSeed>
        {
            new MetricSeed("B3B8371A-5F7D-4D0C-BA4E-BC8FC5D2D406", "B2B182D0-8709-4328-BDA1-0A73B51D0E82", 18),
            new MetricSeed("9C59F504-BFE7-4BA8-A961-312090294BFD", "6D584D3A-6D8E-4B7A-A9AF-2C52C90DAA5E", 12),
            new MetricSeed("D1935CF8-F5AD-4A26-AD67-61FAD368BB12", "B2B182D0-8709-4328-BDA1-0A73B51D0E82", 14),
        };
        metrics.AddRange(notes.Skip(4).Select((note, index) => new MetricSeed(note.Id, index % 3 == 0 ? "6D584D3A-6D8E-4B7A-A9AF-2C52C90DAA5E" : "B2B182D0-8709-4328-BDA1-0A73B51D0E82", index % 3 == 0 ? 8 + index % 7 : 10 + index % 12)));
        foreach (var metric in metrics)
            await ExecuteAsync(connection, transaction, """
                IF NOT EXISTS (SELECT 1 FROM dbo.StudyNoteMetrics WHERE StudyNoteId = @studyNoteId AND MetricDefinitionId = @metricDefinitionId)
                INSERT INTO dbo.StudyNoteMetrics (StudyNoteId, MetricDefinitionId, MetricValue)
                VALUES (@studyNoteId, @metricDefinitionId, @metricValue);
                """,
                ("@studyNoteId", Guid.Parse(metric.StudyNoteId)),
                ("@metricDefinitionId", Guid.Parse(metric.MetricDefinitionId)),
                ("@metricValue", metric.Value));

        await SeedGoalsAsync(connection, transaction);
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
    private sealed record TopicSeed(string Id, string Name);
    private sealed record NoteSeed(string Id, string SubjectId, string Title, string Content, long StudyDurationTicks, string StudyStartedAtUtc);
    private sealed record ConnectionSeed(string Id, string SubjectId, string ConnectedSubjectId);
    private sealed record MetricSeed(string StudyNoteId, string MetricDefinitionId, decimal Value);
    private sealed record GoalSeed(string Id, string SubjectId, string Title, byte Kind, string? MetricDefinitionId, decimal? TargetValue, DateOnly? TargetDate, byte Period, DateOnly? PeriodStartDate, DateOnly? PeriodEndDate, bool IsCompleted, DateTimeOffset? CompletedAtUtc, DateTimeOffset CreatedAtUtc);

    private static NoteSeed Note(string key, string subjectId, string title, string content, int minutes, string studyStartedAtUtc) => new(SeedId($"note-{key}"), subjectId, title, content, TimeSpan.FromMinutes(minutes).Ticks, studyStartedAtUtc);
    private static string SeedId(string key) => new Guid(MD5.HashData(Encoding.UTF8.GetBytes($"knowledge-tracker-seed:{key}"))).ToString();

    private static async Task SeedGoalsAsync(SqlConnection connection, SqlTransaction transaction)
    {
        var goals = new[]
        {
            new GoalSeed(SeedId("goal-csharp-weekly"), "D9674C7B-D6AC-44A2-9531-E22972518F10", "Read 30 pages of C# each week", 1, "B2B182D0-8709-4328-BDA1-0A73B51D0E82", 30, null, 2, null, null, false, null, DateTimeOffset.Parse("2026-06-15T00:00:00+00:00")),
            new GoalSeed(SeedId("goal-sql-daily"), SeedId("subject-sql-server"), "Complete 10 SQL exercises daily", 1, "6D584D3A-6D8E-4B7A-A9AF-2C52C90DAA5E", 10, null, 1, null, null, false, null, DateTimeOffset.Parse("2026-07-01T00:00:00+00:00")),
            new GoalSeed(SeedId("goal-german-monthly"), SeedId("subject-german"), "Read 80 German vocabulary pages monthly", 1, "B2B182D0-8709-4328-BDA1-0A73B51D0E82", 80, null, 3, null, null, false, null, DateTimeOffset.Parse("2026-06-01T00:00:00+00:00")),
            new GoalSeed(SeedId("goal-algorithms-project"), SeedId("subject-algorithms"), "Finish the algorithms practice set", 2, null, null, new DateOnly(2026, 8, 22), 0, null, null, false, null, DateTimeOffset.Parse("2026-07-20T00:00:00+00:00")),
            new GoalSeed(SeedId("goal-networking-review"), SeedId("subject-networking"), "Prepare the networking revision notes", 2, null, null, new DateOnly(2026, 8, 9), 0, null, null, false, null, DateTimeOffset.Parse("2026-07-10T00:00:00+00:00")),
        };
        foreach (var goal in goals)
            await ExecuteAsync(connection, transaction, """
                IF NOT EXISTS (SELECT 1 FROM dbo.SubjectGoals WHERE Id = @id)
                INSERT INTO dbo.SubjectGoals (Id, SubjectId, TopicId, Title, GoalKind, MetricDefinitionId, TargetValue, TargetDate, GoalPeriod, CustomPeriodStartDate, CustomPeriodEndDate, IsCompleted, CompletedAtUtc, CreatedAtUtc)
                VALUES (@id, @subjectId, @subjectId, @title, @kind, @metricDefinitionId, @targetValue, @targetDate, @period, @periodStartDate, @periodEndDate, @isCompleted, @completedAtUtc, @createdAtUtc);
                """,
                ("@id", Guid.Parse(goal.Id)), ("@subjectId", Guid.Parse(goal.SubjectId)), ("@title", goal.Title), ("@kind", goal.Kind), ("@metricDefinitionId", (object?)(goal.MetricDefinitionId is null ? null : Guid.Parse(goal.MetricDefinitionId)) ?? DBNull.Value), ("@targetValue", (object?)goal.TargetValue ?? DBNull.Value), ("@targetDate", (object?)(goal.TargetDate?.ToDateTime(TimeOnly.MinValue)) ?? DBNull.Value), ("@period", goal.Period), ("@periodStartDate", (object?)(goal.PeriodStartDate?.ToDateTime(TimeOnly.MinValue)) ?? DBNull.Value), ("@periodEndDate", (object?)(goal.PeriodEndDate?.ToDateTime(TimeOnly.MinValue)) ?? DBNull.Value), ("@isCompleted", goal.IsCompleted), ("@completedAtUtc", (object?)goal.CompletedAtUtc ?? DBNull.Value), ("@createdAtUtc", goal.CreatedAtUtc));

        foreach (var (title, isCompleted) in new[] { ("Implement sorting exercises", true), ("Solve graph traversal exercises", true), ("Review dynamic programming", false), ("Write solution notes", false) })
            await ExecuteAsync(connection, transaction, """
                IF NOT EXISTS (SELECT 1 FROM dbo.SubjectSubGoals WHERE Id = @id)
                INSERT INTO dbo.SubjectSubGoals (Id, SubjectGoalId, Title, IsCompleted, CompletedAtUtc, CreatedAtUtc)
                VALUES (@id, @subjectGoalId, @title, @isCompleted, @completedAtUtc, @createdAtUtc);
                """,
                ("@id", Guid.Parse(SeedId($"sub-goal-{title}"))), ("@subjectGoalId", Guid.Parse(SeedId("goal-algorithms-project"))), ("@title", title), ("@isCompleted", isCompleted), ("@completedAtUtc", isCompleted ? DateTimeOffset.Parse("2026-08-06T18:00:00+00:00") : DBNull.Value), ("@createdAtUtc", DateTimeOffset.Parse("2026-07-20T00:00:00+00:00")));
    }
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
