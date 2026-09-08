namespace KnowledgeTracker.Application.Knowledge;

// These records are the application boundary: they expose workspace identity
// and presentation data without leaking the Domain entity into transport layers.
public sealed record CreateWorkspaceRequest(string Name);
public sealed record WorkspaceDetails(Guid Id, string Name, DateTimeOffset CreatedAtUtc);
