using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Domain.Knowledge;
using Xunit;

namespace KnowledgeTracker.Tests.Knowledge;

public sealed class SubjectConnectionServiceTests
{
    [Fact]
    public async Task Delete_removes_an_existing_link_and_reports_missing_links()
    {
        var subject = new Subject("Subject");
        var connectedSubject = new Subject("Connected subject");
        var connection = new SubjectConnection(subject.Id, connectedSubject.Id);
        var repository = new FakeConnectionRepository(connection);
        var service = new SubjectConnectionService(
            new FakeSubjectRepository(subject, connectedSubject),
            repository);

        Assert.True(await service.DeleteAsync(connection.Id, CancellationToken.None));
        Assert.Contains(connection.Id, repository.DeletedIds);
        Assert.False(await service.DeleteAsync(Guid.NewGuid(), CancellationToken.None));
    }

    private sealed class FakeConnectionRepository(params SubjectConnection[] initial) : ISubjectConnectionRepository
    {
        private readonly Dictionary<Guid, SubjectConnection> items = initial.ToDictionary(connection => connection.Id);
        public List<Guid> DeletedIds { get; } = [];

        public Task<SubjectConnection?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(items.GetValueOrDefault(id));
        public Task<bool> ExistsAsync(Guid subjectId, Guid connectedSubjectId, CancellationToken ct) => Task.FromResult(items.Values.Any(connection => connection.SubjectId == subjectId && connection.ConnectedSubjectId == connectedSubjectId));
        public Task<IReadOnlyCollection<SubjectConnection>> ListBySubjectAsync(Guid subjectId, CancellationToken ct) => Task.FromResult<IReadOnlyCollection<SubjectConnection>>(items.Values.Where(connection => connection.SubjectId == subjectId || connection.ConnectedSubjectId == subjectId).ToArray());
        public Task AddAsync(SubjectConnection connection, CancellationToken ct) { items[connection.Id] = connection; return Task.CompletedTask; }
        public Task DeleteAsync(Guid id, CancellationToken ct) { items.Remove(id); DeletedIds.Add(id); return Task.CompletedTask; }
    }

    private sealed class FakeSubjectRepository(params Subject[] initial) : ISubjectRepository
    {
        private readonly Dictionary<Guid, Subject> items = initial.ToDictionary(subject => subject.Id);

        public Task<Subject?> FindAsync(Guid id, CancellationToken ct) => Task.FromResult(items.GetValueOrDefault(id));
        public Task<IReadOnlyCollection<Subject>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<Subject>>(items.Values.ToArray());
        public Task<bool> HasChildrenAsync(Guid subjectId, CancellationToken ct) => Task.FromResult(items.Values.Any(subject => subject.ParentSubjectId == subjectId));
        public Task AddAsync(Subject subject, CancellationToken ct) { items[subject.Id] = subject; return Task.CompletedTask; }
        public Task UpdateAsync(Subject subject, CancellationToken ct) { items[subject.Id] = subject; return Task.CompletedTask; }
        public Task DeleteAsync(Guid id, CancellationToken ct) { items.Remove(id); return Task.CompletedTask; }
    }
}
