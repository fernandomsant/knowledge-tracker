namespace KnowledgeTracker.Application.Authentication;

public interface IUserPermissionService
{
    Task<IReadOnlySet<string>> GetPermissionsAsync(Guid userId, CancellationToken ct);
}
