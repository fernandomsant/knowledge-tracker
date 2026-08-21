using System.Text.Json;
using KnowledgeTracker.Application.Knowledge;
using KnowledgeTracker.Web.Knowledge.Contracts;
using KnowledgeTracker.Web.Knowledge.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeTracker.Web.Knowledge.Controllers;

[ApiController]
[Authorize]
public sealed class ClassificationUpdatesController(IClassificationUpdateService updates)
    : ControllerBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan RefreshDelay = TimeSpan.FromMilliseconds(400);

    [HttpGet("api/study-notes/classification-events")]
    public async Task StreamAsync(
        [FromQuery] DateTimeOffset? sinceUtc,
        [FromQuery] Guid? afterJobId,
        CancellationToken ct
    )
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Append("X-Accel-Buffering", "no");
        await Response.StartAsync(ct);
        await Response.WriteAsync(": connected\n\n", ct);
        await Response.Body.FlushAsync(ct);

        var checkpoint = new ClassificationUpdateCheckpoint(
            sinceUtc ?? DateTimeOffset.UtcNow,
            afterJobId ?? Guid.Empty
        );

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var batch = await updates.ListAfterAsync(checkpoint, 32, ct);
                foreach (var update in batch)
                {
                    checkpoint = update.Checkpoint;
                    if (update.Note is null)
                        continue;

                    var response = new ClassificationUpdateResponse(
                        checkpoint.CompletedAtUtc,
                        checkpoint.JobId,
                        KnowledgeResponseMapper.ToResponse(update.Note)
                    );
                    var payload = JsonSerializer.Serialize(response, SerializerOptions);
                    await Response.WriteAsync($"event: note-classification\ndata: {payload}\n\n", ct);
                }

                if (batch.Count > 0)
                    await Response.Body.FlushAsync(ct);

                await Task.Delay(RefreshDelay, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // The client closed the event stream.
        }
    }
}
