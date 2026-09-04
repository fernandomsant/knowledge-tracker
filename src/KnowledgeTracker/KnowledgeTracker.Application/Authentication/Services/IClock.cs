namespace KnowledgeTracker.Application.Authentication;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
