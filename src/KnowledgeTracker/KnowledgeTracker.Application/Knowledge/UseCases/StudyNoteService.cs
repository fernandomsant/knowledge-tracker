using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed class StudyNoteService(
    ISubjectRepository subjects,
    ITopicRepository topics,
    IStudyNoteRepository studyNotes,
    IStudyMetricDefinitionRepository metricDefinitions,
    ISubjectGoalActivityService goalActivity
)
    : IStudyNoteService
{
    public async Task<IReadOnlyCollection<StudyNoteDetails>> ListAsync(CancellationToken ct) =>
        (await studyNotes.ListAsync(ct)).Select(KnowledgeContractMapper.ToDetails).ToArray();

    public async Task<IReadOnlyCollection<StudyNoteDetails>> ListBySubjectAsync(
        Guid subjectId,
        CancellationToken ct
    ) =>
        (await studyNotes.ListBySubjectAsync(subjectId, ct))
            .Select(KnowledgeContractMapper.ToDetails)
            .ToArray();

    public async Task<IReadOnlyCollection<StudyNoteDetails>> ListBySubjectTreeAsync(
        Guid subjectId,
        CancellationToken ct
    ) =>
        (await studyNotes.ListBySubjectTreeAsync(subjectId, ct))
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
        if (await subjects.HasChildrenAsync(subjectId, ct))
            throw new ArgumentException("Only leaf subjects can own study notes.", nameof(subjectId));
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
        await goalActivity.ReevaluateMetricGoalsAsync(subjectId, ct, DateOnly.FromDateTime(studyNote.StudyStartedAtUtc.UtcDateTime));
        return KnowledgeContractMapper.ToDetails(studyNote);
    }

    public async Task<StudyNoteDetails> CreateUnclassifiedAsync(
        CreateUnclassifiedStudyNoteRequest request,
        CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        var studyNote = new StudyNote(
            Guid.NewGuid(),
            null,
            null,
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
        Guid? topicId = null;
        if (studyNote.SubjectId is Guid subjectId)
        {
            if (!request.TopicId.HasValue || request.TopicId.Value == Guid.Empty)
                throw new ArgumentException("A topic must be selected.", nameof(request));
            topicId = request.TopicId.Value;
            var topic = await topics.FindAsync(topicId.Value, ct);
            if (topic is null || topic.SubjectId != subjectId)
                throw new ArgumentException("The selected topic does not belong to this subject.", nameof(request));
        }
        else if (request.TopicId.HasValue)
        {
            throw new ArgumentException("An unclassified note cannot be assigned without an owning subject.", nameof(request));
        }

        var updated = new StudyNote(studyNote.Id, studyNote.SubjectId, topicId,
            request.Title,
            request.Content,
            request.StudyDuration,
            request.StudyStartedAtUtc,
            await CreateMetricsAsync(request.Metrics, ct),
            studyNote.Version + 1,
            NoteClassificationState.Pending
        );
        await studyNotes.UpdateAsync(updated, ct);
        if (studyNote.SubjectId is Guid updatedSubjectId)
        {
            await goalActivity.ReevaluateMetricGoalsAsync(updatedSubjectId, ct, DateOnly.FromDateTime(updated.StudyStartedAtUtc.UtcDateTime));
            if (DateOnly.FromDateTime(studyNote.StudyStartedAtUtc.UtcDateTime) != DateOnly.FromDateTime(updated.StudyStartedAtUtc.UtcDateTime))
                await goalActivity.ReevaluateMetricGoalsAsync(updatedSubjectId, ct, DateOnly.FromDateTime(studyNote.StudyStartedAtUtc.UtcDateTime));
        }
        return KnowledgeContractMapper.ToDetails(updated);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var studyNote = await studyNotes.FindAsync(id, ct);
        if (studyNote is null)
            return false;

        await studyNotes.DeleteAsync(id, ct);
        if (studyNote.SubjectId is Guid subjectId)
            await goalActivity.ReevaluateMetricGoalsAsync(subjectId, ct, DateOnly.FromDateTime(studyNote.StudyStartedAtUtc.UtcDateTime));
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
