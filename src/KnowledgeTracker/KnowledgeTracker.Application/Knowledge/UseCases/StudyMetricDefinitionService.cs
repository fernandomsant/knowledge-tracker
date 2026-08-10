using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed class StudyMetricDefinitionService(IStudyMetricDefinitionRepository definitions)
    : IStudyMetricDefinitionService
{
    public async Task<IReadOnlyCollection<StudyMetricDefinitionDetails>> ListAsync(CancellationToken ct) =>
        (await definitions.ListAsync(ct)).Select(ToDetails).ToArray();

    public async Task<StudyMetricDefinitionDetails> CreateAsync(
        CreateStudyMetricDefinitionRequest request,
        CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        var definition = new StudyMetricDefinition(Guid.NewGuid(), request.Name, request.NumberKind);
        if (await definitions.FindByNormalizedNameAsync(definition.NormalizedName, ct) is not null)
            throw new ArgumentException("A study metric with this name already exists.", nameof(request));

        await definitions.AddAsync(definition, ct);
        return ToDetails(definition);
    }

    private static StudyMetricDefinitionDetails ToDetails(StudyMetricDefinition definition) =>
        new(definition.Id, definition.Name, definition.NumberKind);
}
