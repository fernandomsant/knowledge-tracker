namespace KnowledgeTracker.Application.Knowledge;

public interface ITopicService
{
    Task<IReadOnlyCollection<TopicDetails>> ListAsync(CancellationToken ct);
    Task<TopicDetails> CreateAsync(CreateTopicRequest request, CancellationToken ct);
    Task<TopicDetails?> UpdateAsync(Guid id, UpdateTopicRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
