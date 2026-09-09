namespace KnowledgeTracker.Mcp;

/// <summary>Metric value supplied by an MCP client while creating a study note.</summary>
public sealed record McpStudyNoteMetric(Guid DefinitionId, decimal Value);
