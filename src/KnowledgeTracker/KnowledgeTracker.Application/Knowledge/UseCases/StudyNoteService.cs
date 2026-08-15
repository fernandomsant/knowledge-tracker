using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed class StudyNoteService(
    ISubjectRepository subjects,
    ITopicRepository topics,
    IStudyNoteRepository studyNotes,
    IStudyMetricDefinitionRepository metricDefinitions
)
    : IStudyNoteService
{
    public async Task<IReadOnlyCollection<StudyNoteDetails>> ListBySubjectAsync(
        Guid subjectId,
        CancellationToken ct
    ) =>
        (await studyNotes.ListBySubjectAsync(subjectId, ct))
            .Select(KnowledgeContractMapper.ToDetails)
            .ToArray();

    public async Task<StudyNoteDetails?> CreateAsync(
        Guid subjectId,
        CreateStudyNoteRequest request,
        CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        var subject = await subjects.FindAsync(subjectId, ct);
        if (subject is null)
            return null;
        if (request.TopicId == Guid.Empty)
            throw new ArgumentException("A topic must be selected.", nameof(request));
        var topicId = request.TopicId;
        var topic = await topics.FindAsync(topicId, ct);
        if (topic is null || topic.SubjectId != subjectId)
            throw new ArgumentException("The selected topic does not belong to this subject.", nameof(request));

        var studyNote = subject.AddStudyNote(
            topicId,
            request.Title,
            request.Content,
            request.StudyDuration,
            request.StudyStartedAtUtc,
            await CreateMetricsAsync(request.Metrics, ct)
        );
        await studyNotes.AddAsync(studyNote, ct);
        return KnowledgeContractMapper.ToDetails(studyNote);
    }

    public async Task<StudyNoteDetails?> UpdateAsync(
        Guid id,
        UpdateStudyNoteRequest request,
        CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        var studyNote = await studyNotes.FindAsync(id, ct);
        if (studyNote is null)
            return null;
        if (request.TopicId == Guid.Empty)
            throw new ArgumentException("A topic must be selected.", nameof(request));
        var topicId = request.TopicId;
        var topic = await topics.FindAsync(topicId, ct);
        if (topic is null || topic.SubjectId != studyNote.SubjectId)
            throw new ArgumentException("The selected topic does not belong to this subject.", nameof(request));

        var updated = new StudyNote(studyNote.Id, studyNote.SubjectId, topicId,
            request.Title,
            request.Content,
            request.StudyDuration,
            request.StudyStartedAtUtc,
            await CreateMetricsAsync(request.Metrics, ct)
        );
        await studyNotes.UpdateAsync(updated, ct);
        return KnowledgeContractMapper.ToDetails(updated);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        if (await studyNotes.FindAsync(id, ct) is null)
            return false;

        await studyNotes.DeleteAsync(id, ct);
        return true;
    }

    private async Task<IReadOnlyCollection<StudyNoteMetric>> CreateMetricsAsync(
        IReadOnlyCollection<StudyNoteMetricRequest> requests,
        CancellationToken ct
    )
    {
        var definitions = await Task.WhenAll(requests.Select(async request =>
            await metricDefinitions.FindAsync(request.DefinitionId, ct)
                ?? throw new ArgumentException("A selected study metric does not exist.", nameof(requests))
        ));
        return requests.Select((request, index) => new StudyNoteMetric(definitions[index], request.Value)).ToArray();
    }

}
