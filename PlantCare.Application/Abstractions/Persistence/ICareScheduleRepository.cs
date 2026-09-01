using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Application.Abstractions.Persistence;

public interface ICareScheduleRepository
{
    Task<CareSchedule?> GetForUserAsync(
        Guid userId,
        Guid userPlantId,
        CareActionType actionType,
        CancellationToken cancellationToken = default);
}