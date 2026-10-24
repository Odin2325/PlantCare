using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Application.Notifications;
using Microsoft.AspNetCore.RateLimiting;

namespace PlantCare.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(INotificationService notificationService, IReminderSnoozeService snoozeService) : ControllerBase
{
    public sealed record SnoozeRequest(int Hours);

    [HttpPost("{notificationId:guid}/snooze")]
    [EnableRateLimiting("reminder-snooze")]
    public async Task<IActionResult> Snooze(Guid notificationId, SnoozeRequest request, CancellationToken ct)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        if (request.Hours is not (1 or 24 or 72)) return BadRequest(new ProblemDetails { Title = "Choose 1 hour, 24 hours or 3 days." });
        var result = await snoozeService.SnoozeAsync(userId, notificationId, request.Hours, ct);
        return result.Status switch
        {
            SnoozeStatus.Success => Ok(result.Notification),
            SnoozeStatus.NotFound => NotFound(),
            _ => Conflict(new ProblemDetails { Title = "This reminder is no longer available to snooze. Reload to see current care." })
        };
    }
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> Get([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        if (take is < 1 or > 100) return BadRequest(new ProblemDetails { Title = "Invalid take.", Detail = "take must be between 1 and 100." });
        return Ok(await notificationService.GetForUserAsync(userId, take, cancellationToken));
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return await notificationService.MarkReadAsync(notificationId, userId, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<NotificationPreferenceDto>> GetPreferences(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        return Ok(await notificationService.GetPreferencesAsync(
            userId,
            cancellationToken));
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<NotificationPreferenceDto>> UpdatePreferences(
        UpdateNotificationPreferenceCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        if (command.ReminderLeadTimeHours is not (0 or 24 or 48 or 72 or 168))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid reminder lead time.",
                Detail = "Choose 0, 24, 48, 72, or 168 hours."
            });
        }

        return Ok(await notificationService.UpdatePreferencesAsync(
            userId,
            command,
            cancellationToken));
    }

    private bool TryGetCurrentUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
