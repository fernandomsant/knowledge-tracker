using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Domain.Knowledge;
using Xunit;

namespace KnowledgeTracker.Tests.Knowledge;

public sealed class SubjectNoteOwnershipTests
{
    [Fact]
    public async Task Create_rejects_parent_subjects_and_allows_leaf_subjects()
    {
        var root = new Subject("Root");
        var leaf = new Subject("Leaf", parentSubjectId: root.Id);
        var topic = new Topic(Guid.NewGuid(), leaf.Id, "Reading");
        var subjects = new FakeSubjectRepository(root, leaf);
        var notes = new FakeStudyNoteRepository();
        var service = CreateNoteService(subjects, notes, topic);
        var request = CreateRequest(topic.Id);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(root.Id, request, CancellationToken.None));
        Assert.Empty(notes.Added);

        var created = await service.CreateAsync(leaf.Id, request, CancellationToken.None);

        Assert.NotNull(created);
        Assert.Single(notes.Added);
        Assert.Equal(leaf.Id, notes.Added[0].SubjectId);
    }

    [Fact]
    public async Task Reparenting_rejects_a_subject_with_direct_notes_as_the_new_parent()
    {
        var moving = new Subject("Moving");
        var occupied = new Subject("Occupied");
        var topic = new Topic(Guid.NewGuid(), occupied.Id, "Reading");
        var occupiedNote = occupied.AddStudyNote(topic.Id, "Existing", "Content", TimeSpan.FromMinutes(5), DateTimeOffset.UtcNow);
        var subjects = new FakeSubjectRepository(moving, occupied);
        var notes = new FakeStudyNoteRepository(occupiedNote);
        var service = new SubjectService(subjects, notes, new FakeSubjectLayoutRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(
            moving.Id,
            new UpdateSubjectRequest(moving.Name, moving.Description, occupied.Id),
            CancellationToken.None));

        Assert.Null(subjects.Find(moving.Id)!.ParentSubjectId);
    }

    [Fact]
    public async Task Subject_details_use_recursive_notes_without_changing_direct_note_queries()
    {
        var root = new Subject("Root");
        var leaf = new Subject("Leaf", parentSubjectId: root.Id);
        var topic = new Topic(Guid.NewGuid(), leaf.Id, "Reading");
        var note = leaf.AddStudyNote(topic.Id, "Leaf note", "Content", TimeSpan.FromMinutes(5), DateTimeOffset.UtcNow);
        var subjects = new FakeSubjectRepository(root, leaf);
        var notes = new FakeStudyNoteRepository(note);
        var service = new SubjectService(subjects, notes, new FakeSubjectLayoutRepository());
        var noteService = CreateNoteService(subjects, notes, topic);

        var details = await service.GetAsync(root.Id, CancellationToken.None);
        var direct = await notes.ListBySubjectAsync(root.Id, CancellationToken.None);
        var recursive = await noteService.ListBySubjectTreeAsync(root.Id, CancellationToken.None);

        Assert.Single(details!.StudyNotes);
        Assert.Equal(note.Id, details.StudyNotes.Single().Id);
        Assert.Empty(direct);
        Assert.Single(recursive);
    }

    private static StudyNoteService CreateNoteService(FakeSubjectRepository subjects, FakeStudyNoteRepository notes, Topic topic) =>
        new(subjects, new FakeTopicRepository(topic), notes, new FakeMetricDefinitionRepository(), new FakeGoalActivityService());

    private static CreateStudyNoteRequest CreateRequest(Guid topicId) =>
        new(topicId, "A note", "Content", TimeSpan.FromMinutes(5), DateTimeOffset.UtcNow, []);

    private sealed class FakeSubjectRepository(params Subject[] initial) : ISubjectRepository
    {
        private readonly Dictionary<Guid, Subject> items = initial.ToDictionary(subject => subject.Id);

        public Subject? Find(Guid id) => items.GetValueOrDefault(id);
        public Task<Subject?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(items.GetValueOrDefault(id));
        public Task<IReadOnlyCollection<Subject>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<Subject>>(items.Values.ToArray());
        public Task<bool> HasChildrenAsync(Guid subjectId, CancellationToken ct) => Task.FromResult(items.Values.Any(subject => subject.ParentSubjectId == subjectId));
        public Task AddAsync(Subject subject, CancellationToken ct) { items[subject.Id] = subject; return Task.CompletedTask; }
        public Task UpdateAsync(Subject subject, CancellationToken ct) { items[subject.Id] = subject; return Task.CompletedTask; }
        public Task DeleteAsync(Guid id, CancellationToken ct) { items.Remove(id); return Task.CompletedTask; }
    }

    private sealed class FakeStudyNoteRepository(params StudyNote[] initial) : IStudyNoteRepository
    {
        private readonly List<StudyNote> items = [.. initial];
        public IReadOnlyList<StudyNote> Added => items;
        public Task<StudyNote?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(items.SingleOrDefault(note => note.Id == id));
        public Task<IReadOnlyCollection<StudyNote>> ListBySubjectAsync(Guid subjectId, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<StudyNote>>(items.Where(note => note.SubjectId == subjectId).ToArray());
        public Task<IReadOnlyCollection<StudyNote>> ListBySubjectTreeAsync(Guid subjectId, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<StudyNote>>(items.ToArray());
        public Task AddAsync(StudyNote studyNote, CancellationToken ct) { items.Add(studyNote); return Task.CompletedTask; }
        public Task UpdateAsync(StudyNote studyNote, CancellationToken ct) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeTopicRepository(params Topic[] initial) : ITopicRepository
    {
        private readonly Dictionary<Guid, Topic> items = initial.ToDictionary(topic => topic.Id);
        public Task<Topic?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(items.GetValueOrDefault(id));
        public Task<IReadOnlyCollection<Topic>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<Topic>>(items.Values.ToArray());
        public Task AddAsync(Topic topic, CancellationToken ct) { items[topic.Id] = topic; return Task.CompletedTask; }
        public Task UpdateAsync(Topic topic, CancellationToken ct) => Task.CompletedTask;
        public Task<bool> IsInUseAsync(Guid id, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> DeleteAsync(Guid id, CancellationToken ct) => Task.FromResult(items.Remove(id));
    }

    private sealed class FakeMetricDefinitionRepository : IStudyMetricDefinitionRepository
    {
        public Task<StudyMetricDefinition?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult<StudyMetricDefinition?>(null);
        public Task<StudyMetricDefinition?> FindByNormalizedNameAsync(string normalizedName, CancellationToken ct) => Task.FromResult<StudyMetricDefinition?>(null);
        public Task<IReadOnlyCollection<StudyMetricDefinition>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<StudyMetricDefinition>>([]);
        public Task AddAsync(StudyMetricDefinition definition, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeSubjectLayoutRepository : ISubjectLayoutRepository
    {
        public Task<IReadOnlyCollection<SubjectLayoutPosition>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<SubjectLayoutPosition>>([]);
        public Task UpsertAsync(IReadOnlyCollection<SubjectLayoutPosition> positions, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeGoalActivityService : ISubjectGoalActivityService
    {
        public Task<IReadOnlyCollection<GoalActivityDetails>> GetAsync(DateOnly from, DateOnly to, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<GoalActivityDetails>>([]);
        public Task ReevaluateMetricGoalsAsync(Guid subjectId, CancellationToken ct, DateOnly? affectedDate = null) => Task.CompletedTask;
    }
}
