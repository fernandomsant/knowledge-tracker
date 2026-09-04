using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Application.Authentication;

public interface IMcpAccessTokenRepository
{
    Task<McpAccessToken?> FindByTokenIdentifierAsync(string tokenIdentifier, CancellationToken ct);
    Task<IReadOnlyCollection<McpAccessToken>> ListByUserAsync(Guid userId, CancellationToken ct);
    Task AddAsync(McpAccessToken token, CancellationToken ct);
}
