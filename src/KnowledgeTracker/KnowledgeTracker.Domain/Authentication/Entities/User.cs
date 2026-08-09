namespace KnowledgeTracker.Domain.Authentication;

public sealed class User
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Login { get; init; } = "";
    public string PasswordHash { get; init; } = "";
    public string NormalizedLogin => Login.Trim().ToUpperInvariant();
}