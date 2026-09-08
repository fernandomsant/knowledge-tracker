using System.ComponentModel.DataAnnotations;

namespace KnowledgeTracker.Web.Authentication.Contracts;

/// <summary>Payload for creating a workspace for the authenticated user.</summary>
public sealed record CreateWorkspaceRequest
{
    [Required]
    [StringLength(256)]
    public required string Name { get; init; }
}

/// <summary>Represents a workspace available to the authenticated user.</summary>
public sealed record WorkspaceResponse(Guid Id, string Name, DateTimeOffset CreatedAtUtc);
