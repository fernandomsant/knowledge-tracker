using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public interface IStudyNoteRepository
{
    Task<StudyNote?> FindAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<StudyNote>> ListBySubjectAsync(Guid subjectId, CancellationToken ct);
    Task<IReadOnlyCollection<StudyNote>> ListBySubjectTreeAsync(Guid subjectId, CancellationToken ct);
    Task AddAsync(StudyNote studyNote, CancellationToken ct);
    Task UpdateAsync(StudyNote studyNote, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
