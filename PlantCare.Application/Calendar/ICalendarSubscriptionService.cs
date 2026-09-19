namespace PlantCare.Application.Calendar;

public interface ICalendarSubscriptionService
{
    Task<CalendarSubscriptionStatusDto> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CalendarSubscriptionCreatedDto> CreateAsync(Guid userId, string urlPrefix, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<string?> GetCalendarAsync(string token, CancellationToken cancellationToken = default);
}
