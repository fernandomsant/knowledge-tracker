using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Web.Authentication.Contracts;
using KnowledgeTracker.Web.Authentication.Controllers;
using KnowledgeTracker.Web.Authentication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using CreateWorkspaceApplicationRequest = KnowledgeTracker.Application.Knowledge.CreateWorkspaceRequest;
using CreateWorkspaceHttpRequest = KnowledgeTracker.Web.Authentication.Contracts.CreateWorkspaceRequest;

namespace KnowledgeTracker.Tests.Authentication;

public sealed class CurrentWorkspaceContextTests
{
    [Fact]
    public void WorkspaceId_ReadsWorkspaceHeader()
    {
        var workspaceId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("X-Workspace-Id", workspaceId.ToString());

        var currentWorkspace = new CurrentWorkspaceContext(new HttpContextAccessor { HttpContext = context });

        Assert.Equal(workspaceId, currentWorkspace.WorkspaceId);
    }

    [Fact]
    public void RequireWorkspaceId_RejectsMissingSelection()
    {
        var currentWorkspace = new CurrentWorkspaceContext(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

        Assert.Throws<UnauthorizedAccessException>(() => currentWorkspace.RequireWorkspaceId());
    }

    [Fact]
    public void RequireWorkspaceId_RejectsMalformedSelection()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("X-Workspace-Id", "not-a-guid");
        var currentWorkspace = new CurrentWorkspaceContext(new HttpContextAccessor { HttpContext = context });

        Assert.Throws<ArgumentException>(() => currentWorkspace.RequireWorkspaceId());
    }
}

public sealed class WorkspacesControllerTests
{
    [Fact]
    public async Task GetAsync_ReturnsNotFoundWhenWorkspaceIsNotOwnedByUser()
    {
        var controller = new WorkspacesController(new FakeWorkspaceService { Workspace = null });

        var result = await controller.GetAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateAsync_ReturnsConflictWhenWorkspaceLimitIsReached()
    {
        var controller = new WorkspacesController(new FakeWorkspaceService
        {
            CreateException = new InvalidOperationException("A user cannot have more than 5 workspaces.")
        });

        var result = await controller.CreateAsync(new CreateWorkspaceHttpRequest { Name = "Sixth" }, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    private sealed class FakeWorkspaceService : IWorkspaceService
    {
        public WorkspaceDetails? Workspace { get; init; }
        public Exception? CreateException { get; init; }

        public Task<IReadOnlyCollection<WorkspaceDetails>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyCollection<WorkspaceDetails>>([]);

        public Task<WorkspaceDetails?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(Workspace);

        public Task<WorkspaceDetails> CreateAsync(CreateWorkspaceApplicationRequest request, CancellationToken ct) =>
            CreateException is not null
                ? Task.FromException<WorkspaceDetails>(CreateException)
                : Task.FromResult(new WorkspaceDetails(Guid.NewGuid(), request.Name, DateTimeOffset.UtcNow));
    }
}
