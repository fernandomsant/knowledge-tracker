namespace KnowledgeTracker.Application.Knowledge;

public interface IStudyMetricDefinitionService
{
    Task<IReadOnlyCollection<StudyMetricDefinitionDetails>> ListAsync(CancellationToken ct);
    Task<StudyMetricDefinitionDetails> CreateAsync(CreateStudyMetricDefinitionRequest request, CancellationToken ct);
}
