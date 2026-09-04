using KnowledgeTracker.Application.Authentication;
using KnowledgeTracker.Domain.Authentication;
using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed class TopicService(ITopicRepository topics, ISubjectRepository subjects, IActionAuthorizationService? authorization = null) : ITopicService
{
    public async Task<IReadOnlyCollection<TopicDetails>> ListAsync(CancellationToken ct)
    {
        authorization?.Demand(McpAccessTokenScopeCatalog.TopicsRead);
        return (await topics.ListAsync(ct)).Select(ToDetails).ToArray();
    }

    public async Task<TopicDetails> CreateAsync(CreateTopicRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        authorization?.Demand(McpAccessTokenScopeCatalog.TopicsWrite);
        if (await subjects.FindAsync(request.SubjectId, ct) is null)
            throw new ArgumentException("The selected subject does not exist.", nameof(request));

        var topic = new Topic(Guid.NewGuid(), request.SubjectId, request.Name);
        await topics.AddAsync(topic, ct);
        return ToDetails(topic);
    }

    public async Task<TopicDetails?> UpdateAsync(Guid id, UpdateTopicRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        authorization?.Demand(McpAccessTokenScopeCatalog.TopicsWrite);
        var topic = await topics.FindAsync(id, ct);
        if (topic is null) return null;
        topic.Rename(request.Name);
        await topics.UpdateAsync(topic, ct);
        return ToDetails(topic);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        authorization?.Demand(McpAccessTokenScopeCatalog.TopicsWrite);
        if (await topics.FindAsync(id, ct) is null)
            return false;
        if (await topics.IsInUseAsync(id, ct))
            throw new InvalidOperationException("A topic with notes or goals cannot be deleted.");

        return await topics.DeleteAsync(id, ct);
    }

    private static TopicDetails ToDetails(Topic topic) => new(topic.Id, topic.SubjectId, topic.Name);
}
