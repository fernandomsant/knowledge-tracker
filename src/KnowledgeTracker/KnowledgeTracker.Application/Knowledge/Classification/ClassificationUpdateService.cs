namespace KnowledgeTracker.Application.Knowledge;

public sealed class ClassificationUpdateService(
    IClassificationUpdateRepository updates,
    IStudyNoteService studyNotes
) : IClassificationUpdateService
{
    public async Task<IReadOnlyCollection<ClassificationUpdateDetails>> ListAfterAsync(
        ClassificationUpdateCheckpoint checkpoint,
        int take,
        CancellationToken ct
    )
    {
        if (take is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(take));

        var changes = await updates.ListAfterAsync(checkpoint, take, ct);
        if (changes.Count == 0)
            return [];

        var notesById = (await studyNotes.ListAsync(ct)).ToDictionary(note => note.Id);
        return changes.Select(change => new ClassificationUpdateDetails(
            new ClassificationUpdateCheckpoint(change.CompletedAtUtc, change.JobId),
            notesById.GetValueOrDefault(change.NoteId)
        )).ToArray();
    }
}
