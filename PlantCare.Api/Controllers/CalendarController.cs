using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Application.Calendar;

namespace PlantCare.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/calendar")]
public sealed class CalendarController(ICalendarService calendarService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CalendarEntryDto>>> Get(
        [FromQuery] DateTimeOffset fromUtc, [FromQuery] DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        if (toUtc <= fromUtc || toUtc - fromUtc > TimeSpan.FromDays(366))
            return BadRequest(new ProblemDetails { Title = "Invalid calendar range.", Detail = "Use an end-exclusive range of no more than 366 days." });
        return Ok(await calendarService.GetEntriesAsync(userId, fromUtc, toUtc, cancellationToken));
    }
}
