using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public interface IStudyMetricDefinitionRepository
{
    Task<StudyMetricDefinition?> FindAsync(Guid id, CancellationToken ct);
    Task<StudyMetricDefinition?> FindByNormalizedNameAsync(string normalizedName, CancellationToken ct);
    Task<IReadOnlyCollection<StudyMetricDefinition>> ListAsync(CancellationToken ct);
    Task AddAsync(StudyMetricDefinition definition, CancellationToken ct);
}
