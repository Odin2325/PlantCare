using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PlantCare.Application.Calendar;
using PlantCare.Domain.Entities;
using PlantCare.Infrastructure.Persistence;

namespace PlantCare.Infrastructure.Calendar;

internal sealed class CalendarInvitationService(PlantCareDbContext db, ICalendarShareEmailSender sender,
    TimeProvider clock) : ICalendarInvitationService
{
    public async Task<IReadOnlyList<CalendarShareDto>> GetOwnedAsync(Guid owner, CancellationToken ct = default)
    {
        var ownerEmail = await db.Users.Where(u => u.Id == owner).Select(u => u.Email!).SingleAsync(ct);
        return (await db.CalendarInvitations.AsNoTracking().Where(i => i.OwnerUserId == owner && i.RevokedAtUtc == null && i.AcceptedAtUtc == null)
            .ToListAsync(ct)).Select(i => Map(i, ownerEmail)).ToList();
    }
    public async Task<CalendarShareDto?> CreateAsync(Guid owner, string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToUpperInvariant();
        var ownerEmail = await db.Users.Where(u => u.Id == owner).Select(u => u.Email!).SingleAsync(ct);
        // Existing accounts use direct sharing. Pending invitations never create users.
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalized, ct)) return null;
        var existing = await db.CalendarInvitations.SingleOrDefaultAsync(i => i.OwnerUserId == owner &&
            i.NormalizedRecipientEmail == normalized && i.RevokedAtUtc == null && i.AcceptedAtUtc == null, ct);
        if (existing is not null) return null;
        var token = Token();
        var invite = CalendarInvitation.Create(owner, email, Hash(token), clock.GetUtcNow());
        db.CalendarInvitations.Add(invite); await db.SaveChangesAsync(ct);
        var sent = await sender.SendInvitationAsync(ownerEmail, invite.RecipientEmail, token, ct);
        return Map(invite, ownerEmail) with { EmailNotificationSent = sent };
    }
    public async Task<bool?> ResendAsync(Guid owner, Guid invitationId, CancellationToken ct = default)
    {
        var invite = await db.CalendarInvitations.SingleOrDefaultAsync(i => i.Id == invitationId && i.OwnerUserId == owner &&
            i.RevokedAtUtc == null && i.AcceptedAtUtc == null, ct);
        if (invite is null) return null;
        var token = Token(); invite.ReplaceToken(Hash(token), clock.GetUtcNow()); await db.SaveChangesAsync(ct);
        var ownerEmail = await db.Users.Where(u => u.Id == owner).Select(u => u.Email!).SingleAsync(ct);
        return await sender.SendInvitationAsync(ownerEmail, invite.RecipientEmail, token, ct);
    }
    public async Task<bool> RevokeAsync(Guid owner, Guid invitationId, CancellationToken ct = default)
    {
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var invite = await db.CalendarInvitations.SingleOrDefaultAsync(i => i.Id == invitationId && i.OwnerUserId == owner && i.RevokedAtUtc == null, ct);
            if (invite is null) return false;
            invite.Revoke(clock.GetUtcNow());
            if (invite.AcceptedShareId is Guid shareId)
                (await db.CalendarShares.SingleOrDefaultAsync(s => s.Id == shareId && s.OwnerUserId == owner, ct))?.Revoke(clock.GetUtcNow());
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return true;
        });
    }
    public async Task<CalendarShareDto?> AcceptAsync(Guid recipient, string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length != 43 || token.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_')) return null;
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var user = await db.Users.SingleOrDefaultAsync(u => u.Id == recipient && u.EmailConfirmed, ct);
            if (user is null) return null;
            var hash = Hash(token); var now = clock.GetUtcNow();
            var invite = await db.CalendarInvitations.SingleOrDefaultAsync(i => i.TokenHash == hash && i.RevokedAtUtc == null &&
                i.AcceptedAtUtc == null && i.ExpiresAtUtc > now && i.NormalizedRecipientEmail == user.NormalizedEmail, ct);
            if (invite is null || invite.OwnerUserId == recipient) return null;
            var share = await db.CalendarShares.SingleOrDefaultAsync(s => s.OwnerUserId == invite.OwnerUserId && s.RecipientUserId == recipient && s.RevokedAtUtc == null, ct);
            if (share is null) { share = CalendarShare.Create(invite.OwnerUserId, recipient, now); db.CalendarShares.Add(share); }
            var ownerEmail = await db.Users.Where(u => u.Id == invite.OwnerUserId).Select(u => u.Email!).SingleAsync(ct);
            invite.Accept(share.Id, now); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return new CalendarShareDto(share.Id, ownerEmail, user.Email!, share.CreatedAtUtc, false);
        });
    }
    private static CalendarShareDto Map(CalendarInvitation i, string owner) => new(i.Id, owner, i.RecipientEmail, i.CreatedAtUtc, true,
        null, true, i.ExpiresAtUtc);
    private static string Token() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
