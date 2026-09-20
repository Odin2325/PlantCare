using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Application.Calendar;

namespace PlantCare.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/calendar/integrations/google")]
public sealed class ExternalCalendarsController(
    IExternalCalendarService externalCalendarService,
    IConfiguration configuration,
    ILogger<ExternalCalendarsController> logger)
    : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<ExternalCalendarStatusDto>> GetStatus(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await externalCalendarService.GetGoogleStatusAsync(
            userId,
            cancellationToken));
    }

    [HttpPost("connect")]
    public ActionResult<object> Connect()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try
        {
            return Ok(new
            {
                authorizationUrl = externalCalendarService
                    .CreateGoogleAuthorizationUrl(userId)
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Google Calendar is unavailable.",
                Detail = exception.Message
            });
        }
    }

    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true ||
            !TryGetUserId(out var userId))
            return RedirectToClient("/login?returnUrl=/calendar");

        if (!string.IsNullOrWhiteSpace(error) ||
            string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(state))
            return RedirectToClient("/calendar?google=cancelled");

        try
        {
            await externalCalendarService.CompleteGoogleAuthorizationAsync(
                userId,
                code,
                state,
                cancellationToken);
            return RedirectToClient("/calendar?google=connected");
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
            HttpRequestException or
            System.Security.Cryptography.CryptographicException)
        {
            logger.LogWarning(
                exception,
                "Google Calendar authorization failed for user {UserId}.",
                userId);
            return RedirectToClient("/calendar?google=error");
        }
    }

    [HttpPost("sync")]
    public async Task<ActionResult<ExternalCalendarSyncResultDto>> Sync(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try
        {
            return Ok(await externalCalendarService.SyncGoogleAsync(
                userId,
                cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Google Calendar could not be synchronized.",
                Detail = exception.Message
            });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await externalCalendarService.DisconnectGoogleAsync(
            userId,
            cancellationToken);
        return NoContent();
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);

    private RedirectResult RedirectToClient(string path)
    {
        var clientBaseUrl = configuration["Application:ClientBaseUrl"]
            ?.TrimEnd('/');
        return Redirect($"{clientBaseUrl}{path}");
    }
}
