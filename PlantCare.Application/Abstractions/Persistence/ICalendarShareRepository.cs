using PlantCare.Domain.Entities;

namespace PlantCare.Application.Abstractions.Persistence;

public sealed record CalendarShareRecord(CalendarShare Share, string OwnerEmail, string RecipientEmail);

public interface ICalendarShareRepository
{
    Task<Guid?> FindUserIdByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<CalendarShare?> GetActiveAsync(Guid ownerUserId, Guid recipientUserId, CancellationToken cancellationToken = default);
    Task<CalendarShare?> GetOwnedAsync(Guid shareId, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<CalendarShare?> GetReceivedAsync(Guid shareId, Guid recipientUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CalendarShareRecord>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(CalendarShare share);
}
