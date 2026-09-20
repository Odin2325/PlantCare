using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PlantCare.Application.Calendar;

namespace PlantCare.Api.Controllers;

[ApiController]
[Route("api/calendar/subscription")]
public sealed class CalendarSubscriptionsController(ICalendarSubscriptionService service) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<CalendarSubscriptionStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await service.GetStatusAsync(userId, cancellationToken));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<CalendarSubscriptionCreatedDto>> Create(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var prefix = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/calendar/subscription";
        return Ok(await service.CreateAsync(userId, prefix, cancellationToken));
    }

    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> Revoke(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return await service.RevokeAsync(userId, cancellationToken) ? NoContent() : NotFound();
    }

    [AllowAnonymous]
    [EnableRateLimiting("public-feed")]
    [HttpGet("{token}.ics")]
    public async Task<IActionResult> Download(string token, CancellationToken cancellationToken)
    {
        var calendar = await service.GetCalendarAsync(token, cancellationToken);
        if (calendar is null) return NotFound();
        Response.Headers.CacheControl = "no-store";
        return Content(calendar, "text/calendar; charset=utf-8");
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
