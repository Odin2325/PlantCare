namespace PlantCare.Application.Calendar;

public interface ICalendarService
{
    Task<IReadOnlyList<CalendarEntryDto>> GetEntriesAsync(Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);
}
