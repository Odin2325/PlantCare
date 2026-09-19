using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;

namespace PlantCare.Application.Calendar;

internal sealed class CalendarShareService(ICalendarShareRepository repository, ICalendarService calendarService, IUnitOfWork unitOfWork, TimeProvider timeProvider) : ICalendarShareService
{
    public async Task<IReadOnlyList<CalendarShareDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await repository.GetForUserAsync(userId, cancellationToken)).Select(item => Map(item, userId)).ToList();

    public async Task<CalendarShareDto?> CreateAsync(Guid ownerUserId, string recipientEmail, CancellationToken cancellationToken = default)
    {
        var normalized = recipientEmail?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized)) throw new ArgumentException("A recipient email is required.", nameof(recipientEmail));
        var recipientId = await repository.FindUserIdByEmailAsync(normalized, cancellationToken);
        if (recipientId is null || recipientId == ownerUserId) return null;
        var existing = await repository.GetActiveAsync(ownerUserId, recipientId.Value, cancellationToken);
        if (existing is not null) return null;
        var share = CalendarShare.Create(ownerUserId, recipientId.Value, timeProvider.GetUtcNow());
        repository.Add(share); await unitOfWork.SaveChangesAsync(cancellationToken);
        return new(share.Id, string.Empty, recipientEmail.Trim(), share.CreatedAtUtc, true);
    }

    public async Task<bool> RevokeAsync(Guid shareId, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var share = await repository.GetOwnedAsync(shareId, ownerUserId, cancellationToken);
        if (share is null) return false;
        share.Revoke(timeProvider.GetUtcNow()); await unitOfWork.SaveChangesAsync(cancellationToken); return true;
    }

    public async Task<IReadOnlyList<CalendarEntryDto>?> GetEntriesAsync(Guid shareId, Guid recipientUserId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default)
    {
        var share = await repository.GetReceivedAsync(shareId, recipientUserId, cancellationToken);
        return share is null ? null : await calendarService.GetEntriesAsync(share.OwnerUserId, fromUtc, toUtc, cancellationToken);
    }

    private static CalendarShareDto Map(CalendarShareRecord item, Guid userId) => new(item.Share.Id, item.OwnerEmail, item.RecipientEmail, item.Share.CreatedAtUtc, item.Share.OwnerUserId == userId);
}
