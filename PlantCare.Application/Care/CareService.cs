using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Application.Care;

internal sealed class CareService(
    ICareScheduleRepository careScheduleRepository,
    ICareEventRepository careEventRepository,
    IUserPlantRepository userPlantRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICareService
{
    public async Task<CareScheduleDto?> AddScheduleAsync(
        Guid userId,
        Guid userPlantId,
        CareActionType actionType,
        int intervalDays,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifiers(userId, userPlantId, actionType);
        ValidateInterval(intervalDays);

        if (actionType == CareActionType.Watering)
        {
            throw new InvalidOperationException(
                "A watering schedule already exists for every plant.");
        }

        var userPlant = await userPlantRepository
            .GetTrackedByIdForUserAsync(
                userPlantId,
                userId,
                cancellationToken);

        if (userPlant is null || !userPlant.IsActive)
        {
            return null;
        }

        var schedule = userPlant.AddOrRestoreCareSchedule(
            actionType,
            intervalDays,
            timeProvider.GetUtcNow());

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MapSchedule(schedule);
    }

    public async Task<bool> ArchiveScheduleAsync(
        Guid userId,
        Guid userPlantId,
        CareActionType actionType,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifiers(userId, userPlantId, actionType);

        var userPlant = await userPlantRepository
            .GetTrackedByIdForUserAsync(
                userPlantId,
                userId,
                cancellationToken);

        if (userPlant is null || !userPlant.IsActive)
        {
            return false;
        }

        userPlant.ArchiveCareSchedule(actionType);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CareScheduleDto?> UpdateScheduleAsync(
        Guid userId,
        Guid userPlantId,
        CareActionType actionType,
        int intervalDays,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifiers(userId, userPlantId, actionType);

        ValidateInterval(intervalDays);

        var schedule =
            await careScheduleRepository.GetForUserAsync(
                userId,
                userPlantId,
                actionType,
                cancellationToken);

        if (schedule is null)
        {
            return null;
        }

        schedule.UpdateInterval(intervalDays);

        if (isEnabled)
        {
            schedule.Enable();
        }
        else
        {
            schedule.Disable();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MapSchedule(schedule);
    }

    public async Task<CompleteCareActionResult?> CompleteAsync(
    Guid userId,
    Guid userPlantId,
    CareActionType actionType,
    DateTimeOffset? completedAtUtc,
    string? notes,
    CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid user ID must be provided.",
                nameof(userId));
        }

        if (userPlantId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid user plant ID must be provided.",
                nameof(userPlantId));
        }

        if (actionType == CareActionType.Unknown ||
            !Enum.IsDefined(typeof(CareActionType), actionType))
        {
            throw new ArgumentException(
                "A valid care action type must be provided.",
                nameof(actionType));
        }

        var schedule =
            await careScheduleRepository.GetForUserAsync(
                userId,
                userPlantId,
                actionType,
                cancellationToken);

        if (schedule is null)
        {
            return null;
        }

        var recordedAtUtc =
            timeProvider.GetUtcNow();

        var effectiveCompletedAtUtc =
            completedAtUtc?.ToUniversalTime()
            ?? recordedAtUtc;

        if (effectiveCompletedAtUtc > recordedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(completedAtUtc),
                "A care action cannot be completed in the future.");
        }

        schedule.MarkCompleted(
            effectiveCompletedAtUtc);

        var careEvent = CareEvent.Create(
            careScheduleId: schedule.Id,
            completedAtUtc: effectiveCompletedAtUtc,
            recordedAtUtc: recordedAtUtc,
            notes: notes);

        careEventRepository.Add(careEvent);

        await unitOfWork.SaveChangesAsync(
            cancellationToken);

        return new CompleteCareActionResult(
            Schedule: MapSchedule(schedule),
            Event: new CareEventDto(
                Id: careEvent.Id,
                CareScheduleId:
                    careEvent.CareScheduleId,
                CompletedAtUtc:
                    careEvent.CompletedAtUtc,
                RecordedAtUtc:
                    careEvent.RecordedAtUtc,
                Notes:
                    careEvent.Notes));
    }

    private static void ValidateIdentifiers(
        Guid userId,
        Guid userPlantId,
        CareActionType actionType)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid user ID must be provided.",
                nameof(userId));
        }

        if (userPlantId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid user plant ID must be provided.",
                nameof(userPlantId));
        }

        if (actionType == CareActionType.Unknown ||
            !Enum.IsDefined(typeof(CareActionType), actionType))
        {
            throw new ArgumentException(
                "A valid care action type must be provided.",
                nameof(actionType));
        }
    }

    private static void ValidateInterval(int intervalDays)
    {
        if (intervalDays is < 1 or > 3_650)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intervalDays),
                "The interval must be between 1 and 3650 days.");
        }
    }

    public async Task<IReadOnlyList<CareEventHistoryDto>>
    GetHistoryAsync(Guid userId, Guid userPlantId, int take, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid user ID must be provided.",
                nameof(userId));
        }

        if (userPlantId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid user plant ID must be provided.",
                nameof(userPlantId));
        }

        if (take is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(take),
                "Take must be between 1 and 100.");
        }

        var events =
            await careEventRepository.GetForUserPlantAsync(
                userId,
                userPlantId,
                take,
                cancellationToken);

        return events
            .Select(careEvent =>
                new CareEventHistoryDto(
                    Id: careEvent.Id,
                    CareScheduleId:
                        careEvent.CareScheduleId,
                    ActionType:
                        careEvent.CareSchedule.ActionType,
                    CompletedAtUtc:
                        careEvent.CompletedAtUtc,
                    RecordedAtUtc:
                        careEvent.RecordedAtUtc,
                    Notes:
                        careEvent.Notes))
            .ToList();
    }

    private static CareScheduleDto MapSchedule(CareSchedule schedule)
    {
        return new CareScheduleDto(
            Id: schedule.Id,
            ActionType: schedule.ActionType,
            IntervalDays: schedule.IntervalDays,
            LastCompletedAtUtc: schedule.LastCompletedAtUtc,
            NextDueAtUtc: schedule.NextDueAtUtc,
            IsEnabled: schedule.IsEnabled);
    }
}
