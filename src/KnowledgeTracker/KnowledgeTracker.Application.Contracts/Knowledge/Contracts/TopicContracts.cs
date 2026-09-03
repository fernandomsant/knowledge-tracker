namespace KnowledgeTracker.Application.Knowledge;

public sealed record CreateTopicRequest(Guid SubjectId, string Name);
public sealed record UpdateTopicRequest(string Name);
public sealed record TopicDetails(Guid Id, Guid SubjectId, string Name);
