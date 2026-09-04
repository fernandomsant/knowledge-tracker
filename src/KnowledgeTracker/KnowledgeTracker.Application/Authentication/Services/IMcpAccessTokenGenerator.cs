namespace KnowledgeTracker.Application.Authentication;

public interface IMcpAccessTokenGenerator
{
    McpAccessTokenMaterial Create();
}

public sealed record McpAccessTokenMaterial(
    string TokenIdentifier,
    string Secret,
    string AccessToken
);
