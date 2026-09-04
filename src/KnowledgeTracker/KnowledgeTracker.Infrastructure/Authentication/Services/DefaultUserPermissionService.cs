using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Domain.Authentication;

namespace KnowledgeTracker.Infrastructure.Authentication.Services;

public sealed class DefaultUserPermissionService : IUserPermissionService
{
    public Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken ct) =>
        // The current user model has no persisted permission assignments; every known action is owner-authorized.
        Task.FromResult<IReadOnlySet<string>>(McpAccessTokenScopeCatalog.KnownScopes.ToHashSet(StringComparer.Ordinal));
}
