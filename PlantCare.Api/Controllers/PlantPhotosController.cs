using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Security;
using PlantCare.Infrastructure.Photos;
using PlantCare.PhotoTransport;
using PlantCare.Api.Photos;
using Microsoft.AspNetCore.RateLimiting;

namespace PlantCare.Api.Controllers;

[ApiController, Authorize, Route("api/my-plants/{plantId:guid}/photos")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.Client)]
[PrivatePhotoResponse]
public sealed class PlantPhotosController(PhotoJournalStore store, PhotoJournalOptions options,
    PhotoUploadAdmission admission, IPhotoProcessor processor) : ControllerBase
{
    private Guid Owner => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
    public sealed record EditPhotoRequest(Guid Version, DateOnly PhotographedOn, string? Notes);
    [HttpGet]
    public async Task<IActionResult> List(Guid plantId, bool oldestFirst, CancellationToken ct)
    {
        if (Owner == Guid.Empty) return Unauthorized();
        var items = await store.ListAsync(Owner, plantId, oldestFirst, ct);
        return items is null ? NotFound() : Ok(items);
    }
    [HttpGet("{id:guid}/image")]
    public Task<IActionResult> Image(Guid plantId, Guid id, CancellationToken ct) => Content(plantId, id, false, ct);
    [HttpGet("{id:guid}/thumbnail")]
    public Task<IActionResult> Thumbnail(Guid plantId, Guid id, CancellationToken ct) => Content(plantId, id, true, ct);
    private async Task<IActionResult> Content(Guid plantId, Guid id, bool thumbnail, CancellationToken ct)
    {
        if (Owner == Guid.Empty) return Unauthorized();
        var bytes = await store.ReadAsync(Owner, plantId, id, thumbnail, ct);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return bytes is null ? NotFound() : File(bytes, "image/jpeg");
    }
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Edit(Guid plantId, Guid id, EditPhotoRequest request, CancellationToken ct)
    {
        if (Owner == Guid.Empty) return Unauthorized();
        var item = await store.EditAsync(Owner, plantId, id, request.Version, request.PhotographedOn, request.Notes, ct);
        return item is null ? NotFound() : Ok(item);
    }
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid plantId, Guid id, CancellationToken ct)
    {
        if (Owner == Guid.Empty) return Unauthorized();
        return await store.DeleteAsync(Owner, plantId, id, ct) ? NoContent() : NotFound();
    }
    [HttpGet("capabilities")]
    public async Task<IActionResult> Capabilities(Guid plantId, CancellationToken ct)
    {
        if (Owner == Guid.Empty) return Unauthorized();
        var active = await store.AccessAsync(Owner, plantId, ct);
        return active is null ? NotFound() : Ok(new { uploadsEnabled = options.UploadsEnabled,
            canUpload = options.UploadsEnabled && active.Value, maximumFileBytes = 8 * 1024 * 1024 });
    }
    [HttpGet("requests/{requestId:guid}")]
    public async Task<IActionResult> RequestStatus(Guid plantId, Guid requestId, CancellationToken ct)
    {
        if (Owner == Guid.Empty) return Unauthorized();
        var item = await store.FindRequestAsync(Owner, plantId, requestId, ct);
        return item is null ? NotFound() : Ok(item);
    }
    [HttpPost, StreamingPhotoUpload, Consumes("multipart/form-data")]
    [RequestSizeLimit(PhotoUploadReader.MaximumRequestBytes), EnableRateLimiting("photo-upload")]
    public async Task<IActionResult> Upload(Guid plantId, CancellationToken ct)
    {
        if (Owner == Guid.Empty) return Unauthorized();
        if (!options.UploadsEnabled) return NotFound();
        var active = await store.AccessAsync(Owner, plantId, ct);
        if (active is null) return NotFound();
        if (!active.Value) return Conflict(new ProblemDetails { Title = "Restore this plant before adding a photo." });
        if (Request.ContentLength > PhotoUploadReader.MaximumRequestBytes) return StatusCode(413);
        using var lease = admission.TryEnter();
        if (lease is null) return StatusCode(429);
        try
        {
            using var intake = CancellationTokenSource.CreateLinkedTokenSource(ct);
            intake.CancelAfter(TimeSpan.FromSeconds(20));
            PhotoUpload upload;
            try { upload = await PhotoUploadReader.ReadAsync(Request, intake.Token); }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return StatusCode(408); }
            // Validate user metadata before invoking the isolated decoder.
            PlantCare.Domain.Entities.PlantPhotoEntry.Validate(upload.Date, upload.Notes, DateTimeOffset.UtcNow);
            var photo = await processor.NormalizeAsync(upload.Bytes, upload.Mime, ct);
            var item = await store.SaveNormalizedAsync(Owner, plantId, upload.RequestId, upload.Date, upload.Notes, photo, ct);
            return item is null ? NotFound() : Ok(item);
        }
        catch (UnsupportedPhotoFormatException) { return StatusCode(415); }
        catch (BadHttpRequestException error) { return StatusCode(error.StatusCode); }
        catch (Exception error) when (error is InvalidDataException or System.Text.DecoderFallbackException)
        { return BadRequest(new ProblemDetails { Title = "The photo form is invalid or incomplete." }); }
        catch (PhotoProcessingException error)
        {
            var code = error.Code is "photo-busy" or "photo-too-large" or "photo-format-unsupported" or "photo-processing-timeout"
                or "photo-processing-unavailable" or "photo-processing-failed" or "photo-invalid" or "photo-dimensions-exceeded"
                or "photo-output-too-large" or "photo-animation-unsupported" ? error.Code : "photo-processing-failed";
            var status = code switch { "photo-busy" => 429, "photo-too-large" => 413,
                "photo-format-unsupported" => 415, "photo-processing-timeout" => 504,
                "photo-processing-unavailable" or "photo-processing-failed" => 503, _ => 422 };
            return StatusCode(status, new ProblemDetails { Status = status, Title = "The photo could not be processed.", Detail = code });
        }
    }
}
