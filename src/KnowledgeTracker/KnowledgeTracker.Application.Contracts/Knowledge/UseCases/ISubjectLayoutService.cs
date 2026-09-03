namespace KnowledgeTracker.Application.Knowledge;

public interface ISubjectLayoutService
{
    Task SaveAsync(SaveSubjectLayoutRequest request, CancellationToken ct);
}
