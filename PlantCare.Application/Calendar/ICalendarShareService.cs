namespace PlantCare.Application.Calendar;

public interface ICalendarShareService
{
    Task<IReadOnlyList<CalendarShareDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CalendarShareDto?> CreateAsync(Guid ownerUserId, string recipientEmail, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(Guid shareId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CalendarEntryDto>?> GetEntriesAsync(Guid shareId, Guid recipientUserId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);
}
