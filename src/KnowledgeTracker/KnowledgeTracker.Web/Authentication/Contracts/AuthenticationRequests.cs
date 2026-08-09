using System.ComponentModel.DataAnnotations;

namespace KnowledgeTracker.Web.Authentication.Contracts;

public sealed record RegisterRequest(
    [property: Required, StringLength(256, MinimumLength = 1)] string Login,
    [property: Required, StringLength(1024, MinimumLength = 15)] string Password
);

public sealed record LoginRequest(
    [property: Required, StringLength(256, MinimumLength = 1)] string Login,
    [property: Required, StringLength(1024, MinimumLength = 1)] string Password
);
