using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed class TopicService(ITopicRepository topics) : ITopicService
{
    public async Task<IReadOnlyCollection<TopicDetails>> ListAsync(CancellationToken ct) =>
        (await topics.ListAsync(ct)).Select(ToDetails).ToArray();

    public async Task<TopicDetails> CreateAsync(CreateTopicRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var topic = new Topic(Guid.NewGuid(), request.Name);
        await topics.AddAsync(topic, ct);
        return ToDetails(topic);
    }

    public async Task<TopicDetails?> UpdateAsync(Guid id, UpdateTopicRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var topic = await topics.FindAsync(id, ct);
        if (topic is null) return null;
        topic.Rename(request.Name);
        await topics.UpdateAsync(topic, ct);
        return ToDetails(topic);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => topics.DeleteAsync(id, ct);

    private static TopicDetails ToDetails(Topic topic) => new(topic.Id, topic.Name);
}
