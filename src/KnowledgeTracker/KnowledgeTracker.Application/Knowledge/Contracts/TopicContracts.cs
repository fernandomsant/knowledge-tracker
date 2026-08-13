namespace KnowledgeTracker.Application.Knowledge;

public sealed record CreateTopicRequest(string Name);
public sealed record UpdateTopicRequest(string Name);
public sealed record TopicDetails(Guid Id, string Name);
