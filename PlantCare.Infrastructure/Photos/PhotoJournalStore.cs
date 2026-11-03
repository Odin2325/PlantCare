using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PlantCare.Domain.Entities;
using PlantCare.Infrastructure.Persistence;
using PlantCare.PhotoTransport;

namespace PlantCare.Infrastructure.Photos;

public sealed record PhotoJournalLimits(int PerPlant = 20, int PerAccount = 50,
    long AccountBytes = 20 * 1024 * 1024, long ServiceBytes = 512 * 1024 * 1024);
public sealed record PhotoEntryDto(Guid Id, Guid UserPlantId, DateOnly PhotographedOn, string? Notes,
    DateTimeOffset CreatedAtUtc, Guid Version);
public sealed record PhotoExportItem(PhotoEntryDto Entry, byte[] Image, string Sha256);

public sealed class PhotoJournalStore(PlantCareDbContext db, TimeProvider clock, PhotoJournalLimits limits)
{
    public async Task<PhotoEntryDto?> SaveNormalizedAsync(Guid owner, Guid plantId, Guid requestId,
        DateOnly date, string? notes, ProcessedPhoto photo, CancellationToken ct = default)
    {
        if (limits.PerPlant is < 1 or > 20 || limits.PerAccount is < 1 or > 50 || limits.AccountBytes is < 1 or > 20971520
            || limits.ServiceBytes is < 1 or > 536870912) throw new InvalidOperationException("Invalid photo storage limits.");
        notes = PlantPhotoEntry.Validate(date, notes, clock.GetUtcNow());
        var media = PlantPhotoMedia.Create(Guid.NewGuid(), photo.Image, photo.Thumbnail, checked((int)photo.Width), checked((int)photo.Height));
        var fingerprint = Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {
            media.ImageSha256, media.ThumbnailSha256, media.Width, media.Height, date, notes })));
        var entry = PlantPhotoEntry.Create(plantId, requestId, fingerprint, date, notes, clock.GetUtcNow());
        // IDs share a one-to-one FK; recreate from the already bounded copies.
        media = PlantPhotoMedia.Create(entry.Id, media.Image, media.Thumbnail, media.Width, media.Height);
        return await LockedAsync(async () =>
        {
            if (!await OwnsAsync(owner, plantId, ct)) return null;
            var existing = await db.PlantPhotoEntries.AsNoTracking().SingleOrDefaultAsync(p => p.UserPlantId == plantId && p.UploadRequestId == requestId, ct);
            if (existing is not null)
            {
                if (existing.UploadFingerprint != fingerprint) throw new PhotoJournalConflictException("This upload request was already used for a different photo.");
                return Dto(existing);
            }
            if (!await db.UserPlants.AnyAsync(p => p.Id == plantId && p.IsActive, ct))
                throw new PhotoJournalConflictException("Restore this plant before adding a photo.");
            if (await db.PlantPhotoEntries.IgnoreQueryFilters().CountAsync(p => p.UserPlantId == plantId, ct) >= limits.PerPlant
                || await db.PlantPhotoEntries.IgnoreQueryFilters().CountAsync(p => p.UserPlant.UserId == owner, ct) >= limits.PerAccount)
                throw new PhotoJournalConflictException("The photo journal entry limit has been reached.");
            var accountBytes = await db.PlantPhotoMedia.IgnoreQueryFilters().Where(p => p.Entry.UserPlant.UserId == owner)
                .SumAsync(p => (long?)p.Image.Length + p.Thumbnail.Length, ct) ?? 0;
            var allBytes = await db.PlantPhotoMedia.IgnoreQueryFilters().SumAsync(p => (long?)p.Image.Length + p.Thumbnail.Length, ct) ?? 0;
            var added = (long)media.Image.Length + media.Thumbnail.Length;
            if (accountBytes + added > limits.AccountBytes || allBytes + added > limits.ServiceBytes)
                throw new PhotoJournalConflictException("Photo storage is currently full. Remove a photo or try again later.");
            db.PlantPhotoEntries.Add(entry); db.PlantPhotoMedia.Add(media);
            await db.SaveChangesAsync(ct);
            return Dto(entry);
        }, ct);
    }

    public async Task<IReadOnlyList<PhotoEntryDto>?> ListAsync(Guid owner, Guid plantId, bool oldestFirst, CancellationToken ct)
    {
        if (!await OwnsAsync(owner, plantId, ct)) return null;
        var query = db.PlantPhotoEntries.AsNoTracking().Where(p => p.UserPlantId == plantId);
        var ordered = oldestFirst ? query.OrderBy(p => p.PhotographedOn).ThenBy(p => p.CreatedAtUtc).ThenBy(p => p.Id)
            : query.OrderByDescending(p => p.PhotographedOn).ThenByDescending(p => p.CreatedAtUtc).ThenBy(p => p.Id);
        return await ordered.Take(20).Select(p => new PhotoEntryDto(p.Id, p.UserPlantId, p.PhotographedOn, p.Notes, p.CreatedAtUtc, p.Version)).ToArrayAsync(ct);
    }

    public async Task<byte[]?> ReadAsync(Guid owner, Guid plantId, Guid id, bool thumbnail, CancellationToken ct)
    {
        if (!await OwnsAsync(owner, plantId, ct)) return null;
        var query = db.PlantPhotoMedia.AsNoTracking().Where(p => p.Id == id && p.Entry.UserPlantId == plantId && p.Entry.UserPlant.UserId == owner);
        var result = await query.Select(p => new { Bytes = thumbnail ? p.Thumbnail : p.Image,
            Hash = thumbnail ? p.ThumbnailSha256 : p.ImageSha256 }).SingleOrDefaultAsync(ct);
        if (result is null) return null;
        Verify(result.Bytes, result.Hash, thumbnail ? 65536 : 524288);
        return result.Bytes;
    }

    public Task<PhotoEntryDto?> EditAsync(Guid owner, Guid plantId, Guid id, Guid version,
        DateOnly date, string? notes, CancellationToken ct) => LockedAsync<PhotoEntryDto?>(async () =>
    {
        if (!await OwnsAsync(owner, plantId, ct)) return null;
        var item = await db.PlantPhotoEntries.SingleOrDefaultAsync(p => p.Id == id && p.UserPlantId == plantId, ct);
        if (item is null) return null;
        if (version == Guid.Empty || item.Version != version) throw new PhotoJournalConflictException("This photo entry changed. Refresh before editing again.");
        item.Edit(date, notes, clock.GetUtcNow()); await db.SaveChangesAsync(ct); return Dto(item);
    }, ct);

    public Task<bool> DeleteAsync(Guid owner, Guid plantId, Guid id, CancellationToken ct) => LockedAsync(async () =>
    {
        if (!await OwnsAsync(owner, plantId, ct)) return false;
        await db.PlantPhotoEntries.Where(p => p.Id == id && p.UserPlantId == plantId).ExecuteDeleteAsync(ct);
        return true;
    }, ct);

    public Task<IReadOnlyList<PhotoExportItem>?> ExportAsync(Guid owner, CancellationToken ct) => LockedAsync<IReadOnlyList<PhotoExportItem>?>(async () =>
    {
        if (!await db.Users.AnyAsync(u => u.Id == owner, ct)) return null;
        var query = db.PlantPhotoMedia.AsNoTracking().Where(p => p.Entry.UserPlant.UserId == owner);
        if (await query.CountAsync(ct) > 50 || (await query.SumAsync(p => (long?)p.Image.Length + p.Thumbnail.Length, ct) ?? 0) > 20971520)
            throw new PhotoJournalConflictException("Photo export exceeds its supported size. Contact support.");
        var rows = await query.OrderBy(p => p.Id).Select(p => new { p.Entry, p.Image, p.ImageSha256 }).ToArrayAsync(ct);
        foreach (var row in rows) Verify(row.Image, row.ImageSha256, 524288);
        return rows.Select(p => new PhotoExportItem(Dto(p.Entry), p.Image, p.ImageSha256)).ToArray();
    }, ct);

    private async Task<bool> OwnsAsync(Guid owner, Guid plantId, CancellationToken ct) => owner != Guid.Empty
        && await db.Users.AnyAsync(u => u.Id == owner, ct)
        && await db.UserPlants.AnyAsync(p => p.Id == plantId && p.UserId == owner, ct);

    private async Task<T> LockedAsync<T>(Func<Task<T>> action, CancellationToken ct) =>
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            await PhotoJournalLock.AcquireAsync(db, ct);
            var result = await action();
            await tx.CommitAsync(ct); return result;
        });

    private static PhotoEntryDto Dto(PlantPhotoEntry p) => new(p.Id, p.UserPlantId, p.PhotographedOn, p.Notes, p.CreatedAtUtc, p.Version);
    private static void Verify(byte[] bytes, string expected, int maximum)
    {
        PlantPhotoMedia.ValidateBytes(bytes, maximum);
        if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != expected)
            throw new PhotoJournalConflictException("Photo content failed its integrity check. Contact support.");
    }
}
