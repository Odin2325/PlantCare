using PlantCare.Domain.Enums;

namespace PlantCare.Application.Care;

public interface ICareService
{
    Task<CompleteCareActionResult?> CompleteAsync(
        Guid userId,
        Guid userPlantId,
        CareActionType actionType,
        DateTimeOffset? completedAtUtc,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CareEventHistoryDto>>
        GetHistoryAsync(
            Guid userId,
            Guid userPlantId,
            int take,
            CancellationToken cancellationToken = default);

    Task<CareScheduleDto?> UpdateScheduleAsync(
        Guid userId,
        Guid userPlantId,
        CareActionType actionType,
        int intervalDays,
        bool isEnabled,
        CancellationToken cancellationToken = default);

    Task<CareScheduleDto?> AddScheduleAsync(
        Guid userId,
        Guid userPlantId,
        CareActionType actionType,
        int intervalDays,
        CancellationToken cancellationToken = default);

    Task<bool> ArchiveScheduleAsync(
        Guid userId,
        Guid userPlantId,
        CareActionType actionType,
        CancellationToken cancellationToken = default);
}
