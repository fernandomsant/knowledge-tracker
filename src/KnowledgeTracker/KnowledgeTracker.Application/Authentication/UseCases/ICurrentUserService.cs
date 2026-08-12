namespace KnowledgeTracker.Application.Authentication;

public interface ICurrentUserService
{
    Task<CurrentUser?> GetAsync(Guid id, CancellationToken ct);
}
