using System.ComponentModel.DataAnnotations;

namespace KnowledgeTracker.Web.Authentication.Contracts;

public sealed record RegisterRequest
{
    [Required, StringLength(256, MinimumLength = 1)]
    public required string Login { get; init; }

    [Required, StringLength(1024, MinimumLength = 1)]
    public required string Password { get; init; }
}

public sealed record LoginRequest
{
    [Required, StringLength(256, MinimumLength = 1)]
    public required string Login { get; init; }

    [Required, StringLength(1024, MinimumLength = 1)]
    public required string Password { get; init; }
}
