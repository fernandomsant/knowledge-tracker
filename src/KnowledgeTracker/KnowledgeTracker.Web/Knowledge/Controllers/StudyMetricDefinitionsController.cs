using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Web.Knowledge.Contracts;
using KnowledgeTracker.Web.Knowledge.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeTracker.Web.Knowledge.Controllers;

[ApiController]
[Authorize]
[Route("api/study-metric-definitions")]
public sealed class StudyMetricDefinitionsController(IStudyMetricDefinitionService definitions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<StudyMetricDefinitionResponse>>> ListAsync(CancellationToken ct) =>
        Ok((await definitions.ListAsync(ct)).Select(KnowledgeResponseMapper.ToResponse).ToArray());

    [HttpPost]
    public async Task<ActionResult<StudyMetricDefinitionResponse>> CreateAsync(
        KnowledgeTracker.Web.Knowledge.Contracts.CreateStudyMetricDefinitionRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var definition = await definitions.CreateAsync(
                new KnowledgeTracker.Application.Knowledge.CreateStudyMetricDefinitionRequest(request.Name, request.NumberKind),
                ct
            );
            return Created($"api/study-metric-definitions/{definition.Id}", KnowledgeResponseMapper.ToResponse(definition));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
    }
}
