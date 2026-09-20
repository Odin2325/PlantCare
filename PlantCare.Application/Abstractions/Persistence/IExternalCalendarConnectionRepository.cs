using PlantCare.Domain.Entities;

namespace PlantCare.Application.Abstractions.Persistence;

public interface IExternalCalendarConnectionRepository
{
    Task<ExternalCalendarConnection?> GetGoogleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    void Add(ExternalCalendarConnection connection);
    void Remove(ExternalCalendarConnection connection);
}
