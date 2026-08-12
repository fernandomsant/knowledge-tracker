namespace KnowledgeTracker.Application.Authentication;

public sealed class CurrentUserService(IUserRepository users) : ICurrentUserService
{
    public async Task<CurrentUser?> GetAsync(Guid id, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(id, ct);
        return user is null ? null : new CurrentUser(user.Id, user.Login);
    }
}
