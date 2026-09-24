using Microsoft.EntityFrameworkCore;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Persistence.Repositories;

internal sealed class NotificationPreferenceRepository(
    PlantCareDbContext dbContext) : INotificationPreferenceRepository
{
    public Task<NotificationPreference?> GetAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.NotificationPreferences.SingleOrDefaultAsync(
            preference => preference.UserId == userId,
            cancellationToken);

    public void Add(NotificationPreference preference) =>
        dbContext.NotificationPreferences.Add(preference);
}
