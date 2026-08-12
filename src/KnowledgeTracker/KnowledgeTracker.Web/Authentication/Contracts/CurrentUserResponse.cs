namespace KnowledgeTracker.Web.Authentication.Contracts;

/// <summary>Represents the authenticated user visible to the client application.</summary>
public sealed record CurrentUserResponse(Guid Id, string Login);
