using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Application.Authentication;

public interface IUserRepository
{
    Task<User?> FindAsync(string normalizedLogin, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
}