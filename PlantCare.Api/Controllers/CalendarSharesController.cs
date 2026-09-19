using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Application.Calendar;

namespace PlantCare.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/calendar/shares")]
public sealed class CalendarSharesController(ICalendarShareService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CalendarShareDto>>> Get(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await service.GetForUserAsync(userId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CalendarShareDto>> Create(CreateCalendarShareCommand command, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var share = await service.CreateAsync(userId, command.RecipientEmail, cancellationToken);
        return share is null ? BadRequest(new ProblemDetails { Title = "Calendar could not be shared.", Detail = "Use another registered account that does not already have access." }) : Created($"/api/calendar/shares/{share.Id}", share);
    }

    [HttpDelete("{shareId:guid}")]
    public async Task<IActionResult> Revoke(Guid shareId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return await service.RevokeAsync(shareId, userId, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpGet("{shareId:guid}/entries")]
    public async Task<ActionResult<IReadOnlyList<CalendarEntryDto>>> GetEntries(Guid shareId, [FromQuery] DateTimeOffset fromUtc, [FromQuery] DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (toUtc <= fromUtc || toUtc - fromUtc > TimeSpan.FromDays(366)) return BadRequest();
        var entries = await service.GetEntriesAsync(shareId, userId, fromUtc, toUtc, cancellationToken);
        return entries is null ? NotFound() : Ok(entries);
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
