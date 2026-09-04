namespace KnowledgeTracker.Application.Authentication;

public interface IMcpAccessTokenService
{
    Task<CreateMcpAccessTokenResult> CreateAsync(
        CreateMcpAccessTokenRequest request,
        CancellationToken ct
    );

    Task<IReadOnlyCollection<McpAccessTokenDto>> ListAsync(CancellationToken ct);

    Task<bool> RevokeAsync(Guid id, CancellationToken ct);
}
