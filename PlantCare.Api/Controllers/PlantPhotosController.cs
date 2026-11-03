using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Security;
using PlantCare.Infrastructure.Photos;

namespace PlantCare.Api.Controllers;

[ApiController, Authorize, Route("api/my-plants/{plantId:guid}/photos")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.Client)]
[PrivatePhotoResponse]
public sealed class PlantPhotosController(PhotoJournalStore store) : ControllerBase
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
    // Deliberately no POST/upload action until admission, retention and recovery gates pass.
}
