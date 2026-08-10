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
            subject.ParentSubjectId,
            notes.Select(KnowledgeContractMapper.ToDetails).ToArray()
        );
    }

    public async Task<IReadOnlyCollection<SubjectSummary>> ListAsync(CancellationToken ct) =>
        (await subjects.ListAsync(ct)).Select(KnowledgeContractMapper.ToSummary).ToArray();

    public async Task<SubjectSummary> CreateAsync(CreateSubjectRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        await ValidateParentAsync(null, request.ParentSubjectId, ct);
        var subject = new Subject(request.Name, request.Description, request.ParentSubjectId);
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
        await ValidateParentAsync(id, request.ParentSubjectId, ct);
        subject.SetParent(request.ParentSubjectId);
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

    private async Task ValidateParentAsync(Guid? subjectId, Guid? parentSubjectId, CancellationToken ct)
    {
        if (parentSubjectId is null) return;

        var depth = 0;
        var currentId = parentSubjectId;
        while (currentId is not null)
        {
            if (currentId == subjectId)
                throw new ArgumentException("A subject cannot be its own ancestor.", nameof(parentSubjectId));

            var parent = await subjects.FindAsync(currentId.Value, ct)
                ?? throw new ArgumentException("The selected parent subject does not exist.", nameof(parentSubjectId));
            depth++;
            if (depth > 3)
                throw new ArgumentException("Subjects can have at most four hierarchy levels.", nameof(parentSubjectId));
            currentId = parent.ParentSubjectId;
        }
    }

}
