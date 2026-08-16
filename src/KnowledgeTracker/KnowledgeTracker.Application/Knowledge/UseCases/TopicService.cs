using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed class TopicService(ITopicRepository topics, ISubjectRepository subjects) : ITopicService
{
    public async Task<IReadOnlyCollection<TopicDetails>> ListAsync(CancellationToken ct) =>
        (await topics.ListAsync(ct)).Select(ToDetails).ToArray();

    public async Task<TopicDetails> CreateAsync(CreateTopicRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await subjects.FindAsync(request.SubjectId, ct) is null)
            throw new ArgumentException("The selected subject does not exist.", nameof(request));

        var topic = new Topic(Guid.NewGuid(), request.SubjectId, request.Name);
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

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        if (await topics.FindAsync(id, ct) is null)
            return false;
        if (await topics.IsInUseAsync(id, ct))
            throw new InvalidOperationException("A topic with notes or goals cannot be deleted.");

        return await topics.DeleteAsync(id, ct);
    }

    private static TopicDetails ToDetails(Topic topic) => new(topic.Id, topic.SubjectId, topic.Name);
}
