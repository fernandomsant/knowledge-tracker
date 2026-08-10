using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Web.Knowledge.Contracts;
using KnowledgeTracker.Web.Knowledge.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CreateStudyNoteHttpRequest = KnowledgeTracker.Web.Knowledge.Contracts.CreateStudyNoteRequest;
using UpdateStudyNoteHttpRequest = KnowledgeTracker.Web.Knowledge.Contracts.UpdateStudyNoteRequest;

namespace KnowledgeTracker.Web.Knowledge.Controllers;

[ApiController]
[Authorize]
public sealed class StudyNotesController(ISubjectService subjects, IStudyNoteService studyNotes)
    : ControllerBase
{
    private const string ListStudyNotesBySubjectRoute = "list-study-notes-by-subject";

    [HttpGet("api/subjects/{subjectId:guid}/notes", Name = ListStudyNotesBySubjectRoute)]
    [ProducesResponseType(typeof(IReadOnlyCollection<StudyNoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<StudyNoteResponse>>> ListBySubjectAsync(
        Guid subjectId,
        CancellationToken ct
    )
    {
        var subject = await subjects.GetAsync(subjectId, ct);
        return subject is null
            ? NotFound()
            : Ok(subject.StudyNotes.Select(KnowledgeResponseMapper.ToResponse).ToArray());
    }

    [HttpPost("api/subjects/{subjectId:guid}/notes")]
    [ProducesResponseType(typeof(StudyNoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudyNoteResponse>> CreateAsync(
        Guid subjectId,
        CreateStudyNoteHttpRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var note = await studyNotes.CreateAsync(
                subjectId,
                new KnowledgeTracker.Application.Knowledge.CreateStudyNoteRequest(
                    request.Title,
                    request.Content,
                    request.StudyDuration,
                    request.StudyStartedAtUtc,
                    request.Metrics.Select(metric => new KnowledgeTracker.Application.Knowledge.StudyNoteMetricRequest(metric.DefinitionId, metric.Value)).ToArray()
                ),
                ct
            );
            if (note is null)
                return NotFound();

            var response = KnowledgeResponseMapper.ToResponse(note);
            return CreatedAtRoute(ListStudyNotesBySubjectRoute, new { subjectId }, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
    }

    [HttpPut("api/study-notes/{id:guid}")]
    [ProducesResponseType(typeof(StudyNoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudyNoteResponse>> UpdateAsync(
        Guid id,
        UpdateStudyNoteHttpRequest request,
        CancellationToken ct
    )
    {
        try
        {
            var note = await studyNotes.UpdateAsync(
                id,
                new KnowledgeTracker.Application.Knowledge.UpdateStudyNoteRequest(
                    request.Title,
                    request.Content,
                    request.StudyDuration,
                    request.Metrics.Select(metric => new KnowledgeTracker.Application.Knowledge.StudyNoteMetricRequest(metric.DefinitionId, metric.Value)).ToArray()
                ),
                ct
            );
            return note is null ? NotFound() : Ok(KnowledgeResponseMapper.ToResponse(note));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Detail = exception.Message });
        }
    }

    [HttpDelete("api/study-notes/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct) =>
        await studyNotes.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
