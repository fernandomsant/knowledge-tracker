using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed class SubjectService(ISubjectRepository subjects, IStudyNoteRepository studyNotes)
    : ISubjectService
{
    public async Task<SubjectDetails?> GetAsync(Guid id, CancellationToken ct)
    {
        var subject = await subjects.FindAsync(id, ct);
        if (subject is null)
            return null;

        var notes = await studyNotes.ListBySubjectAsync(id, ct);
        return new SubjectDetails(
            subject.Id,
            subject.Name,
            subject.Description,
            notes.Select(KnowledgeContractMapper.ToDetails).ToArray()
        );
    }

    public async Task<IReadOnlyCollection<SubjectSummary>> ListAsync(CancellationToken ct) =>
        (await subjects.ListAsync(ct)).Select(KnowledgeContractMapper.ToSummary).ToArray();

    public async Task<SubjectSummary> CreateAsync(CreateSubjectRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var subject = new Subject(request.Name, request.Description);
        await subjects.AddAsync(subject, ct);
        return KnowledgeContractMapper.ToSummary(subject);
    }

    public async Task<SubjectSummary?> UpdateAsync(
        Guid id,
        UpdateSubjectRequest request,
        CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        var subject = await subjects.FindAsync(id, ct);
        if (subject is null)
            return null;

        subject.Rename(request.Name);
        subject.UpdateDescription(request.Description);
        await subjects.UpdateAsync(subject, ct);
        return KnowledgeContractMapper.ToSummary(subject);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        if (await subjects.FindAsync(id, ct) is null)
            return false;

        await subjects.DeleteAsync(id, ct);
        return true;
    }

}
