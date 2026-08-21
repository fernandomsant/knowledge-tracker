namespace KnowledgeTracker.Application.Knowledge;

public interface IStudyNoteService
{
    Task<IReadOnlyCollection<StudyNoteDetails>> ListAsync(CancellationToken ct);
    Task<IReadOnlyCollection<StudyNoteDetails>> ListBySubjectAsync(Guid subjectId, CancellationToken ct);
    Task<IReadOnlyCollection<StudyNoteDetails>> ListBySubjectTreeAsync(Guid subjectId, CancellationToken ct);
    Task<StudyNoteDetails?> CreateAsync(Guid subjectId, CreateStudyNoteRequest request, CancellationToken ct);
    Task<StudyNoteDetails> CreateUnclassifiedAsync(CreateUnclassifiedStudyNoteRequest request, CancellationToken ct);
    Task<StudyNoteDetails?> UpdateAsync(Guid id, UpdateStudyNoteRequest request, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
