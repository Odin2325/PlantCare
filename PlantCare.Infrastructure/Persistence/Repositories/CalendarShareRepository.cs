using Microsoft.EntityFrameworkCore;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;
using PlantCare.Infrastructure.Identity;

namespace PlantCare.Infrastructure.Persistence.Repositories;

internal sealed class CalendarShareRepository(PlantCareDbContext dbContext) : ICalendarShareRepository
{
    public Task<Guid?> FindUserIdByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        dbContext.Set<ApplicationUser>().Where(user => user.NormalizedEmail == normalizedEmail).Select(user => (Guid?)user.Id).SingleOrDefaultAsync(cancellationToken);

    public Task<CalendarShare?> GetActiveAsync(Guid ownerUserId, Guid recipientUserId, CancellationToken cancellationToken = default) =>
        dbContext.CalendarShares.SingleOrDefaultAsync(share => share.OwnerUserId == ownerUserId && share.RecipientUserId == recipientUserId && share.RevokedAtUtc == null, cancellationToken);

    public Task<CalendarShare?> GetOwnedAsync(Guid shareId, Guid ownerUserId, CancellationToken cancellationToken = default) =>
        dbContext.CalendarShares.SingleOrDefaultAsync(share => share.Id == shareId && share.OwnerUserId == ownerUserId && share.RevokedAtUtc == null, cancellationToken);

    public Task<CalendarShare?> GetReceivedAsync(Guid shareId, Guid recipientUserId, CancellationToken cancellationToken = default) =>
        dbContext.CalendarShares.AsNoTracking().SingleOrDefaultAsync(share => share.Id == shareId && share.RecipientUserId == recipientUserId && share.RevokedAtUtc == null, cancellationToken);

    public async Task<IReadOnlyList<CalendarShareRecord>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await (from share in dbContext.CalendarShares.AsNoTracking()
               join owner in dbContext.Set<ApplicationUser>() on share.OwnerUserId equals owner.Id
               join recipient in dbContext.Set<ApplicationUser>() on share.RecipientUserId equals recipient.Id
               where share.RevokedAtUtc == null && (share.OwnerUserId == userId || share.RecipientUserId == userId)
               orderby share.CreatedAtUtc descending
               select new CalendarShareRecord(share, owner.Email!, recipient.Email!)).ToListAsync(cancellationToken);

    public void Add(CalendarShare share) => dbContext.CalendarShares.Add(share);
}
