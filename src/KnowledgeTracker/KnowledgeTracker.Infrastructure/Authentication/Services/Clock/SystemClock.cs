using KnowledgeTracker.Application.Authentication;

namespace KnowledgeTracker.Infrastructure.Authentication;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
