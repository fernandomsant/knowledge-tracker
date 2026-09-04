namespace KnowledgeTracker.Domain.Authentication;

public sealed record McpAccessTokenScope
{
    public const int MaximumLength = 128;

    public McpAccessTokenScope(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("MCP access-token scope is required.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaximumLength)
            throw new ArgumentException($"MCP access-token scope must be {MaximumLength} characters or fewer.", nameof(value));
        if (normalized.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not (':' or '-' or '_' or '.')))
            throw new ArgumentException("MCP access-token scope contains invalid characters.", nameof(value));

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public static class McpAccessTokenScopeCatalog
{
    public const string SubjectsRead = "subjects:read";
    public const string SubjectsWrite = "subjects:write";
    public const string TopicsRead = "topics:read";
    public const string TopicsWrite = "topics:write";
    public const string NotesRead = "notes:read";
    public const string NotesWrite = "notes:write";
    public const string GoalsRead = "goals:read";
    public const string GoalsWrite = "goals:write";
    public const string ConnectionsRead = "connections:read";
    public const string ConnectionsWrite = "connections:write";
    public const string LayoutsRead = "layouts:read";
    public const string LayoutsWrite = "layouts:write";
    public const string MetricsRead = "metrics:read";
    public const string MetricsWrite = "metrics:write";
    public const string TokensManage = "tokens:manage";

    private static readonly IReadOnlySet<string> knownScopes = new HashSet<string>(StringComparer.Ordinal)
    {
        SubjectsRead,
        SubjectsWrite,
        TopicsRead,
        TopicsWrite,
        NotesRead,
        NotesWrite,
        GoalsRead,
        GoalsWrite,
        ConnectionsRead,
        ConnectionsWrite,
        LayoutsRead,
        LayoutsWrite,
        MetricsRead,
        MetricsWrite,
        TokensManage,
    };

    public static IReadOnlyCollection<string> KnownScopes => knownScopes;

    public static IReadOnlyCollection<McpAccessTokenScope> Normalize(IEnumerable<string> scopes)
    {
        ArgumentNullException.ThrowIfNull(scopes);

        var normalized = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in scopes)
        {
            var scope = new McpAccessTokenScope(value);
            if (!knownScopes.Contains(scope.Value))
                throw new ArgumentException($"Unknown MCP access-token scope '{scope.Value}'.", nameof(scopes));

            normalized.Add(scope.Value);
        }

        return normalized
            .Order(StringComparer.Ordinal)
            .Select(value => new McpAccessTokenScope(value))
            .ToArray();
    }

    public static bool IsKnown(McpAccessTokenScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return knownScopes.Contains(scope.Value);
    }
}
