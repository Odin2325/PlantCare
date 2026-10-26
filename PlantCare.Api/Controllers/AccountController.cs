using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Contracts.Authentication;
using PlantCare.Infrastructure.Identity;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using PlantCare.Infrastructure.Persistence;

namespace PlantCare.Api.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public sealed class AccountController(UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager, PlantCareDbContext db) : ControllerBase
{
    public sealed record PasswordRequest([Required, StringLength(1024)] string Password);
    public sealed record DeleteRequest([Required, StringLength(1024)] string Password,
        [Required] string Confirmation);

    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> GetMe()
    {
        var user = await userManager.GetUserAsync(User);

        if (user?.Email is null)
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserResponse(Id: user.Id, Email: user.Email));
    }

    [HttpPost("export")]
    [EnableRateLimiting("authentication")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Export(PasswordRequest request, CancellationToken ct)
    {
        var user = await Reauthenticate(request.Password);
        if (user is null) return BadRequest(new ProblemDetails { Title = "Current password could not be verified. Try again later or reset your password." });
        var id = user.Id;
        // Explicit projections exclude credentials, push keys and calendar bearer secrets.
        var plants = db.UserPlants.AsNoTracking().Where(p => p.UserId == id);
        var schedules = db.CareSchedules.AsNoTracking().Where(s => plants.Select(p => p.Id).Contains(s.UserPlantId));
        var notifications = db.Notifications.AsNoTracking().Where(n => n.UserId == id);
        var connections = db.ExternalCalendarConnections.AsNoTracking().Where(c => c.UserId == id);
        var data = new
        {
            schemaVersion = 1, exportedAtUtc = DateTimeOffset.UtcNow,
            account = new { user.Id, user.Email, user.UserName, user.PhoneNumber, user.EmailConfirmed, user.TwoFactorEnabled, user.CreatedAtUtc },
            plants = await plants.Select(p => new { p.Id, p.PlantSpeciesId, p.Nickname, p.Location, p.Notes, p.AcquiredOn, p.PlannedSowingOn, p.PlannedPlantingOutOn, p.ExpectedHarvestOn, p.IsActive, p.CreatedAtUtc }).ToListAsync(ct),
            tags = await db.UserPlantTags.AsNoTracking().Where(t => plants.Select(p => p.Id).Contains(t.UserPlantId)).Select(t => new { t.UserPlantId, t.Name }).ToListAsync(ct),
            schedules = await schedules.Select(s => new { s.Id, s.UserPlantId, s.ActionType, s.IntervalDays, s.ScheduleMode, s.WeekDays, s.PreferredTimeLocal, s.TimeZoneId, s.LastCompletedAtUtc, s.NextDueAtUtc, s.IsEnabled, s.IsArchived }).ToListAsync(ct),
            harvests = await db.HarvestRecords.AsNoTracking().Where(h => plants.Select(p => p.Id).Contains(h.UserPlantId)).Select(h => new { h.Id, h.UserPlantId, h.HarvestedOn, h.Quantity, h.Unit, h.Notes, h.RecordedAtUtc }).ToListAsync(ct),
            history = await db.CareEvents.AsNoTracking().Where(e => schedules.Select(s => s.Id).Contains(e.CareScheduleId)).Select(e => new { e.Id, e.CareScheduleId, e.CompletedAtUtc, e.RecordedAtUtc, e.Notes }).ToListAsync(ct),
            preferences = await db.NotificationPreferences.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == id, ct),
            notifications = await notifications.Select(n => new { n.Id, n.CareScheduleId, n.DueAtUtc, n.CreatedAtUtc, n.ReadAtUtc, n.PushSentAtUtc, n.PushNextAttemptAtUtc, n.ReminderSequence, n.NotBeforeUtc, n.SupersededAtUtc }).ToListAsync(ct),
            pushDevices = await db.PushSubscriptions.AsNoTracking().Where(p => p.UserId == id).Select(p => new { p.Id, p.CreatedAtUtc, p.UpdatedAtUtc }).ToListAsync(ct),
            pushDeliveryHistory = await db.PushDeliveries.AsNoTracking().Where(d => notifications.Select(n => n.Id).Contains(d.NotificationId)).Select(d => new { d.NotificationId, d.SubscriptionId, d.Status, d.AttemptCount, d.LastStatusCode, d.NextAttemptAtUtc, d.FinishedAtUtc }).ToListAsync(ct),
            calendarSubscriptions = await db.CalendarSubscriptions.AsNoTracking().Where(s => s.UserId == id).Select(s => new { s.Id, s.CreatedAtUtc, s.RevokedAtUtc }).ToListAsync(ct),
            calendarShares = await db.CalendarShares.AsNoTracking().Where(s => s.OwnerUserId == id || s.RecipientUserId == id).Select(s => new { s.Id, direction = s.OwnerUserId == id ? "outgoing" : "incoming", s.CreatedAtUtc, s.RevokedAtUtc }).ToListAsync(ct),
            calendarInvitations = await db.CalendarInvitations.AsNoTracking().Where(i => i.OwnerUserId == id || i.NormalizedRecipientEmail == user.NormalizedEmail)
                .Select(i => new { i.Id, i.RecipientEmail, i.CreatedAtUtc, i.ExpiresAtUtc, i.AcceptedAtUtc, i.RevokedAtUtc }).ToListAsync(ct),
            externalCalendars = await connections.Select(c => new { c.Id, c.Provider, c.AccountEmail, c.CreatedAtUtc, c.LastSyncedAtUtc }).ToListAsync(ct),
            externalEvents = await db.ExternalCalendarEvents.AsNoTracking().Where(e => connections.Select(c => c.Id).Contains(e.ConnectionId)).Select(e => new { e.Id, e.ConnectionId, e.PlantCareEntryId, e.ExternalEventId, e.CreatedAtUtc }).ToListAsync(ct),
            roles = await userManager.GetRolesAsync(user),
            claims = (await userManager.GetClaimsAsync(user)).Select(c => new { c.Type, c.Value }),
            excluded = new[] { "Passwords and authentication tokens", "Push endpoints and encryption keys", "Calendar feed tokens/hashes", "External provider access/refresh tokens", "Other users' private records", "Operational backups and provider-held copies" }
        };
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return File(JsonSerializer.SerializeToUtf8Bytes(data, options), "application/json", "plantcare-account.json");
    }

    [HttpPost("delete")]
    [EnableRateLimiting("authentication")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Delete(DeleteRequest request, CancellationToken ct)
    {
        if (request.Confirmation != "DELETE") return BadRequest(new ProblemDetails { Title = "Type DELETE to confirm account deletion." });
        var user = await Reauthenticate(request.Password);
        if (user is null) return BadRequest(new ProblemDetails { Title = "Current password could not be verified. Try again later or reset your password." });
        var deleted = await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            // Disconnect providers first so their remote cleanup is not silently skipped.
            if (await db.ExternalCalendarConnections.AnyAsync(c => c.UserId == user.Id, ct)) return false;
            if (!await db.AccountErasures.AnyAsync(e => e.UserId == user.Id, ct))
            {
                db.AccountErasures.Add(PlantCare.Domain.Entities.AccountErasure.Create(user.Id, DateTimeOffset.UtcNow));
                await db.SaveChangesAsync(ct);
            }
            await db.CalendarShares.IgnoreQueryFilters().Where(s => s.OwnerUserId == user.Id || s.RecipientUserId == user.Id).ExecuteDeleteAsync(ct);
            await db.CalendarInvitations.IgnoreQueryFilters().Where(i => i.OwnerUserId == user.Id || i.NormalizedRecipientEmail == user.NormalizedEmail).ExecuteDeleteAsync(ct);
            // Existing foreign keys cascade plants/history, notification deliveries,
            // preferences, devices, subscriptions and Identity children atomically.
            await db.Users.IgnoreQueryFilters().Where(u => u.Id == user.Id).ExecuteDeleteAsync(ct);
            await transaction.CommitAsync(ct);
            return true;
        });
        if (!deleted) return Conflict(new ProblemDetails { Title = "Disconnect external calendars on the Calendar page before deleting your account." });
        await signInManager.SignOutAsync();
        return NoContent();
    }

    private async Task<ApplicationUser?> Reauthenticate(string password)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return null;
        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        return result.Succeeded ? user : null;
    }
}
