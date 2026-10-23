using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net.Mail;
using PlantCare.Application.Calendar;

namespace PlantCare.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/calendar/shares")]
public sealed class CalendarSharesController(ICalendarShareService service, ICalendarInvitationService invitations) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CalendarShareDto>>> Get(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok((await service.GetForUserAsync(userId, cancellationToken))
            .Concat(await invitations.GetOwnedAsync(userId, cancellationToken)).OrderByDescending(s => s.CreatedAtUtc));
    }

    [HttpPost]
    [EnableRateLimiting("calendar-sharing")]
    public async Task<ActionResult<CalendarShareDto>> Create(CreateCalendarShareCommand command, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (command.RecipientEmail?.Trim().Length > 256 || !MailAddress.TryCreate(command.RecipientEmail?.Trim(), out var address) || address.Address != command.RecipientEmail?.Trim())
            return BadRequest(new ProblemDetails { Title = "Enter a valid recipient email address." });
        var share = await service.CreateAsync(userId, command.RecipientEmail, cancellationToken);
        share ??= await invitations.CreateAsync(userId, command.RecipientEmail, cancellationToken);
        return share is null ? BadRequest(new ProblemDetails { Title = "Calendar could not be shared.", Detail = "Use another email address that does not already have access or a pending invitation." }) : Created($"/api/calendar/shares/{share.Id}", share);
    }

    [HttpPost("{shareId:guid}/email")]
    [EnableRateLimiting("calendar-sharing")]
    public async Task<IActionResult> SendEmail(Guid shareId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var sent = await service.SendEmailAsync(shareId, userId, cancellationToken);
        sent ??= await invitations.ResendAsync(userId, shareId, cancellationToken);
        if (sent is null) return NotFound();
        return sent.Value ? Accepted() : StatusCode(503, new ProblemDetails
        {
            Title = "The email could not be sent.",
            Detail = "Shared access is unchanged. Try sending the email again later."
        });
    }

    [HttpDelete("{shareId:guid}")]
    public async Task<IActionResult> Revoke(Guid shareId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return await service.RevokeAsync(shareId, userId, cancellationToken) || await invitations.RevokeAsync(userId, shareId, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPost("accept-invitation")]
    [EnableRateLimiting("calendar-sharing")]
    public async Task<ActionResult<CalendarShareDto>> Accept(AcceptCalendarInvitationCommand command, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var share = await invitations.AcceptAsync(userId, command.Token, ct);
        return share is null ? BadRequest(new ProblemDetails { Title = "Invitation is unavailable.",
            Detail = "Use the confirmed account for the invited email address. The link may have expired, been used or revoked." }) : Ok(share);
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
