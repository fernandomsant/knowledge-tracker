namespace KnowledgeTracker.Web.Contracts.Authentication;

public sealed record RegisterRequest(string Login, string Password);

public sealed record LoginRequest(string Login, string Password);

public sealed record RefreshRequest(string RefreshToken);
