using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Application.Calendar;

namespace PlantCare.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/calendar/integrations/microsoft")]
public sealed class MicrosoftCalendarController(
    IMicrosoftCalendarService microsoftCalendarService,
    IConfiguration configuration,
    ILogger<MicrosoftCalendarController> logger)
    : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<ExternalCalendarStatusDto>> GetStatus(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await microsoftCalendarService.GetStatusAsync(
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
                authorizationUrl = microsoftCalendarService
                    .CreateAuthorizationUrl(userId)
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Microsoft Calendar is unavailable.",
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
            return RedirectToClient("/calendar?microsoft=cancelled");

        try
        {
            await microsoftCalendarService.CompleteAuthorizationAsync(
                userId,
                code,
                state,
                cancellationToken);
            return RedirectToClient("/calendar?microsoft=connected");
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
            HttpRequestException or
            System.Security.Cryptography.CryptographicException)
        {
            logger.LogWarning(
                exception,
                "Microsoft Calendar authorization failed for user {UserId}.",
                userId);
            return RedirectToClient("/calendar?microsoft=error");
        }
    }

    [HttpPost("sync")]
    public async Task<ActionResult<ExternalCalendarSyncResultDto>> Sync(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try
        {
            return Ok(await microsoftCalendarService.SyncAsync(
                userId,
                cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Microsoft Calendar could not be synchronized.",
                Detail = exception.Message
            });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        await microsoftCalendarService.DisconnectAsync(
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
