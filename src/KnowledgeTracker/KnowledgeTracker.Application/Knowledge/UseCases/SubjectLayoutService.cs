using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed class SubjectLayoutService(ISubjectRepository subjects, ISubjectLayoutRepository layouts)
    : ISubjectLayoutService
{
    public async Task SaveAsync(SaveSubjectLayoutRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Positions);

        var positions = request.Positions
            .Select(position => position is null
                ? throw new ArgumentException("A layout position is required.", nameof(request))
                : new SubjectLayoutPosition(position.SubjectId, position.NormalizedX, position.NormalizedY))
            .ToArray();

        if (positions.GroupBy(position => position.SubjectId).Any(group => group.Skip(1).Any()))
            throw new ArgumentException("A subject can appear only once in a layout save.", nameof(request));
        if (positions.Length == 0)
            return;

        var subjectIds = (await subjects.ListAsync(ct)).Select(subject => subject.Id).ToHashSet();
        var unknownSubjectId = positions.Select(position => position.SubjectId).FirstOrDefault(id => !subjectIds.Contains(id));
        if (unknownSubjectId != Guid.Empty)
            throw new ArgumentException("A subject in the layout does not exist.", nameof(request));

        await layouts.UpsertAsync(positions, ct);
    }
}
