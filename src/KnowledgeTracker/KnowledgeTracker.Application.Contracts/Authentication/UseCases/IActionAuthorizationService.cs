namespace KnowledgeTracker.Application.Authentication;

public interface IActionAuthorizationService
{
    void Demand(string scope);
}
