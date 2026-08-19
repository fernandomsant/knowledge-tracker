using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Domain.Knowledge;
using Xunit;

namespace KnowledgeTracker.Tests.Knowledge;

public sealed class SubjectGoalHierarchyTests
{
    [Fact]
    public async Task Parent_goal_progress_includes_notes_from_all_descendants()
    {
        var root = new Subject("Root");
        var branch = new Subject("Branch", parentSubjectId: root.Id);
        var nestedLeaf = new Subject("Nested leaf", parentSubjectId: branch.Id);
        var siblingLeaf = new Subject("Sibling leaf", parentSubjectId: root.Id);
        var rootTopic = new Topic(Guid.NewGuid(), root.Id, "Overview");
        var nestedTopic = new Topic(Guid.NewGuid(), nestedLeaf.Id, "Nested topic");
        var siblingTopic = new Topic(Guid.NewGuid(), siblingLeaf.Id, "Sibling topic");
        var definition = StudyTimeDefinition();
        var now = DateTimeOffset.UtcNow;
        var nestedNote = nestedLeaf.AddStudyNote(nestedTopic.Id, "Nested", "Content", TimeSpan.FromMinutes(30), now);
        var siblingNote = siblingLeaf.AddStudyNote(siblingTopic.Id, "Sibling", "Content", TimeSpan.FromMinutes(30), now);
        var goal = MetricGoal(root.Id, rootTopic.Id, definition.Id, 1m, GoalPeriod.AllTime, now);
        var subjects = new FakeSubjectRepository(root, branch, nestedLeaf, siblingLeaf);
        var notes = new FakeStudyNoteRepository(subjects.Items, nestedNote, siblingNote);
        var service = CreateGoalService(subjects, notes, definition, rootTopic, nestedTopic, siblingTopic, goal);

        var details = await service.ListBySubjectAsync(root.Id, CancellationToken.None);

        Assert.Equal(1m, Assert.Single(details).CurrentValue);
    }

    [Fact]
    public async Task Leaf_goal_progress_remains_scoped_to_its_topic()
    {
        var leaf = new Subject("Leaf");
        var selectedTopic = new Topic(Guid.NewGuid(), leaf.Id, "Selected");
        var otherTopic = new Topic(Guid.NewGuid(), leaf.Id, "Other");
        var definition = StudyTimeDefinition();
        var now = DateTimeOffset.UtcNow;
        var selectedNote = leaf.AddStudyNote(selectedTopic.Id, "Selected", "Content", TimeSpan.FromMinutes(30), now);
        var otherNote = leaf.AddStudyNote(otherTopic.Id, "Other", "Content", TimeSpan.FromMinutes(30), now);
        var goal = MetricGoal(leaf.Id, selectedTopic.Id, definition.Id, 1m, GoalPeriod.AllTime, now);
        var subjects = new FakeSubjectRepository(leaf);
        var notes = new FakeStudyNoteRepository(subjects.Items, selectedNote, otherNote);
        var service = CreateGoalService(subjects, notes, definition, selectedTopic, otherTopic, goal);

        var details = await service.ListBySubjectAsync(leaf.Id, CancellationToken.None);

        Assert.Equal(0.5m, Assert.Single(details).CurrentValue);
    }

    [Fact]
    public async Task Note_change_in_a_child_reevaluates_and_completes_the_parent_goal()
    {
        var root = new Subject("Root");
        var firstLeaf = new Subject("First leaf", parentSubjectId: root.Id);
        var secondLeaf = new Subject("Second leaf", parentSubjectId: root.Id);
        var rootTopic = new Topic(Guid.NewGuid(), root.Id, "Overview");
        var firstTopic = new Topic(Guid.NewGuid(), firstLeaf.Id, "First topic");
        var secondTopic = new Topic(Guid.NewGuid(), secondLeaf.Id, "Second topic");
        var definition = StudyTimeDefinition();
        var now = DateTimeOffset.UtcNow;
        var firstNote = firstLeaf.AddStudyNote(firstTopic.Id, "First", "Content", TimeSpan.FromMinutes(30), now);
        var secondNote = secondLeaf.AddStudyNote(secondTopic.Id, "Second", "Content", TimeSpan.FromMinutes(30), now);
        var goal = MetricGoal(root.Id, rootTopic.Id, definition.Id, 1m, GoalPeriod.Daily, now);
        var subjects = new FakeSubjectRepository(root, firstLeaf, secondLeaf);
        var notes = new FakeStudyNoteRepository(subjects.Items, firstNote, secondNote);
        var goals = new FakeGoalRepository(goal);
        var completions = new FakeCompletionRepository();
        var service = new SubjectGoalActivityService(
            new FakeGoalActivityRepository(goal),
            completions,
            goals,
            subjects,
            notes,
            new FakeMetricDefinitionRepository(definition));

        await service.ReevaluateMetricGoalsAsync(
            firstLeaf.Id,
            CancellationToken.None,
            DateOnly.FromDateTime(now.UtcDateTime));

        var completion = Assert.Single(completions.Registered);
        Assert.Equal(goal.Id, completion.SubjectGoalId);
        Assert.Equal(DateOnly.FromDateTime(now.UtcDateTime), completion.OccurrenceStartDate);
        Assert.Equal(GoalCompletionSource.Metric, completion.Source);
    }

    private static SubjectGoalService CreateGoalService(
        FakeSubjectRepository subjects,
        FakeStudyNoteRepository notes,
        StudyMetricDefinition definition,
        Topic firstTopic,
        Topic secondTopic,
        SubjectGoal goal) =>
        new(
            new FakeGoalRepository(goal),
            new FakeCompletionRepository(),
            new FakeGoalActivityService(),
            subjects,
            new FakeTopicRepository(firstTopic, secondTopic),
            notes,
            new FakeMetricDefinitionRepository(definition));

    private static SubjectGoalService CreateGoalService(
        FakeSubjectRepository subjects,
        FakeStudyNoteRepository notes,
        StudyMetricDefinition definition,
        Topic firstTopic,
        Topic secondTopic,
        Topic thirdTopic,
        SubjectGoal goal) =>
        new(
            new FakeGoalRepository(goal),
            new FakeCompletionRepository(),
            new FakeGoalActivityService(),
            subjects,
            new FakeTopicRepository(firstTopic, secondTopic, thirdTopic),
            notes,
            new FakeMetricDefinitionRepository(definition));

    private static StudyMetricDefinition StudyTimeDefinition() =>
        new(StandardStudyMetricDefinitionIds.StudyTime, "Study time", MetricNumberKind.Rational);

    private static SubjectGoal MetricGoal(
        Guid subjectId,
        Guid topicId,
        Guid definitionId,
        decimal target,
        GoalPeriod period,
        DateTimeOffset createdAtUtc) =>
        new(
            Guid.NewGuid(),
            subjectId,
            topicId,
            "Study target",
            GoalKind.MetricTarget,
            definitionId,
            target,
            null,
            period,
            null,
            null,
            long.MaxValue,
            false,
            null,
            createdAtUtc);

    private sealed class FakeSubjectRepository(params Subject[] initial) : ISubjectRepository
    {
        private readonly Dictionary<Guid, Subject> items = initial.ToDictionary(subject => subject.Id);
        public IReadOnlyCollection<Subject> Items => items.Values;
        public Task<Subject?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(items.GetValueOrDefault(id));
        public Task<IReadOnlyCollection<Subject>> ListAsync(CancellationToken ct) => Task.FromResult(Items);
        public Task<bool> HasChildrenAsync(Guid subjectId, CancellationToken ct) => Task.FromResult(items.Values.Any(subject => subject.ParentSubjectId == subjectId));
        public Task AddAsync(Subject subject, CancellationToken ct) { items[subject.Id] = subject; return Task.CompletedTask; }
        public Task UpdateAsync(Subject subject, CancellationToken ct) { items[subject.Id] = subject; return Task.CompletedTask; }
        public Task DeleteAsync(Guid id, CancellationToken ct) { items.Remove(id); return Task.CompletedTask; }
    }

    private sealed class FakeStudyNoteRepository(IReadOnlyCollection<Subject> subjects, params StudyNote[] initial) : IStudyNoteRepository
    {
        private readonly List<StudyNote> items = [.. initial];
        public Task<StudyNote?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(items.SingleOrDefault(note => note.Id == id));
        public Task<IReadOnlyCollection<StudyNote>> ListBySubjectAsync(Guid subjectId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<StudyNote>>(items.Where(note => note.SubjectId == subjectId).ToArray());
        public Task<IReadOnlyCollection<StudyNote>> ListBySubjectTreeAsync(Guid subjectId, CancellationToken ct)
        {
            var subjectIds = new HashSet<Guid> { subjectId };
            while (subjects.Where(subject => subject.ParentSubjectId is not null && subjectIds.Contains(subject.ParentSubjectId.Value)).Select(subject => subject.Id).Where(subjectIds.Add).Any())
            {
            }
            return Task.FromResult<IReadOnlyCollection<StudyNote>>(items.Where(note => subjectIds.Contains(note.SubjectId)).ToArray());
        }
        public Task AddAsync(StudyNote studyNote, CancellationToken ct) { items.Add(studyNote); return Task.CompletedTask; }
        public Task UpdateAsync(StudyNote studyNote, CancellationToken ct) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken ct) { items.RemoveAll(note => note.Id == id); return Task.CompletedTask; }
    }

    private sealed class FakeGoalRepository(params SubjectGoal[] initial) : ISubjectGoalRepository
    {
        private readonly Dictionary<Guid, SubjectGoal> items = initial.ToDictionary(goal => goal.Id);
        public Task<IReadOnlyCollection<SubjectGoal>> ListBySubjectAsync(Guid subjectId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<SubjectGoal>>(items.Values.Where(goal => goal.SubjectId == subjectId).ToArray());
        public Task<SubjectGoal?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(items.GetValueOrDefault(id));
        public Task AddAsync(SubjectGoal goal, CancellationToken ct) { items[goal.Id] = goal; return Task.CompletedTask; }
        public Task UpdateAsync(SubjectGoal goal, IReadOnlyCollection<SubjectSubGoal> subGoals, CancellationToken ct) { items[goal.Id] = goal; return Task.CompletedTask; }
        public Task<bool> DeleteAsync(Guid id, DateTimeOffset deactivatedAtUtc, CancellationToken ct) => Task.FromResult(items.Remove(id));
        public Task<bool> CompleteAsync(Guid id, DateTimeOffset completedAtUtc, CancellationToken ct) => Task.FromResult(false);
        public Task<SubjectSubGoal?> FindSubGoalAsync(Guid id, CancellationToken ct) => Task.FromResult<SubjectSubGoal?>(null);
        public Task AddSubGoalsAsync(IReadOnlyCollection<SubjectSubGoal> subGoals, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyCollection<SubjectSubGoal>> ListSubGoalsAsync(IReadOnlyCollection<Guid> subjectGoalIds, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<SubjectSubGoal>>([]);
        public Task<bool> SetSubGoalCompletionAsync(Guid id, bool isCompleted, DateTimeOffset changedAtUtc, CancellationToken ct) => Task.FromResult(false);
        public Task<bool> SwapPriorityAsync(Guid id, Guid swapWithId, CancellationToken ct) => Task.FromResult(false);
    }

    private sealed class FakeCompletionRepository : ISubjectGoalCompletionRepository
    {
        public List<SubjectGoalCompletion> Registered { get; } = [];
        public Task<IReadOnlyCollection<SubjectGoalCompletion>> ListAsync(IReadOnlyCollection<Guid> goalIds, DateOnly from, DateOnly to, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<SubjectGoalCompletion>>(Registered);
        public Task RegisterAsync(SubjectGoalCompletion completion, CancellationToken ct) { Registered.Add(completion); return Task.CompletedTask; }
        public Task RemoveAsync(Guid goalId, DateOnly occurrenceStartDate, DateOnly occurrenceEndDate, CancellationToken ct) { Registered.RemoveAll(completion => completion.SubjectGoalId == goalId && completion.OccurrenceStartDate == occurrenceStartDate && completion.OccurrenceEndDate == occurrenceEndDate); return Task.CompletedTask; }
    }

    private sealed class FakeGoalActivityRepository(params SubjectGoal[] goals) : ISubjectGoalActivityRepository
    {
        public Task<IReadOnlyCollection<SubjectGoal>> ListForPeriodAsync(DateOnly from, DateOnly to, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<SubjectGoal>>(goals);
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

    private sealed class FakeMetricDefinitionRepository(params StudyMetricDefinition[] initial) : IStudyMetricDefinitionRepository
    {
        private readonly Dictionary<Guid, StudyMetricDefinition> items = initial.ToDictionary(definition => definition.Id);
        public Task<StudyMetricDefinition?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(items.GetValueOrDefault(id));
        public Task<StudyMetricDefinition?> FindByNormalizedNameAsync(string normalizedName, CancellationToken ct) => Task.FromResult(items.Values.SingleOrDefault(definition => definition.NormalizedName == normalizedName));
        public Task<IReadOnlyCollection<StudyMetricDefinition>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<StudyMetricDefinition>>(items.Values.ToArray());
        public Task AddAsync(StudyMetricDefinition definition, CancellationToken ct) { items[definition.Id] = definition; return Task.CompletedTask; }
    }

    private sealed class FakeGoalActivityService : ISubjectGoalActivityService
    {
        public Task<IReadOnlyCollection<GoalActivityDetails>> GetAsync(DateOnly from, DateOnly to, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<GoalActivityDetails>>([]);
        public Task ReevaluateMetricGoalsAsync(Guid subjectId, CancellationToken ct, DateOnly? affectedDate = null) => Task.CompletedTask;
    }
}
