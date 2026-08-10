using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed record CreateStudyMetricDefinitionRequest(string Name, MetricNumberKind NumberKind);
public sealed record StudyMetricDefinitionDetails(Guid Id, string Name, MetricNumberKind NumberKind);
