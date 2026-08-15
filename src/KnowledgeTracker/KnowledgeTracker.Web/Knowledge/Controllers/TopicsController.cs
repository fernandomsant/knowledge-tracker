using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Web.Knowledge.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeTracker.Web.Knowledge.Controllers;

[ApiController, Authorize, Route("api/topics")]
public sealed class TopicsController(ITopicService topics) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyCollection<TopicResponse>>> ListAsync(CancellationToken ct) => Ok((await topics.ListAsync(ct)).Select(topic => new TopicResponse(topic.Id, topic.SubjectId, topic.Name)).ToArray());
    [HttpPost("/api/subjects/{subjectId:guid}/topics")] public async Task<ActionResult<TopicResponse>> CreateAsync(Guid subjectId, KnowledgeTracker.Web.Knowledge.Contracts.CreateTopicRequest request, CancellationToken ct) { try { var topic = await topics.CreateAsync(new(subjectId, request.Name), ct); return Created($"/api/topics/{topic.Id}", new TopicResponse(topic.Id, topic.SubjectId, topic.Name)); } catch (ArgumentException error) { return BadRequest(new ProblemDetails { Detail = error.Message }); } }
}
