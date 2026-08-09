using KnowledgeTracker.Domain.Knowledge;

namespace KnowledgeTracker.Application.Knowledge;

public sealed class SubjectConnectionService(
    ISubjectRepository subjects,
    ISubjectConnectionRepository connections
) : ISubjectConnectionService
{
    public async Task<IReadOnlyCollection<SubjectConnectionDetails>> ListBySubjectAsync(
        Guid subjectId,
        CancellationToken ct
    ) =>
        (await connections.ListBySubjectAsync(subjectId, ct))
            .Select(KnowledgeContractMapper.ToDetails)
            .ToArray();

    public async Task<SubjectConnectionDetails?> CreateAsync(
        CreateSubjectConnectionRequest request,
        CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        var connection = new SubjectConnection(request.SubjectId, request.ConnectedSubjectId);
        if (
            await subjects.FindAsync(connection.SubjectId, ct) is null
            || await subjects.FindAsync(connection.ConnectedSubjectId, ct) is null
        )
            return null;

        if (await connections.ExistsAsync(connection.SubjectId, connection.ConnectedSubjectId, ct))
            throw new InvalidOperationException("The subject connection already exists.");

        await connections.AddAsync(connection, ct);
        return KnowledgeContractMapper.ToDetails(connection);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        if (await connections.FindAsync(id, ct) is null)
            return false;

        await connections.DeleteAsync(id, ct);
        return true;
    }

}
