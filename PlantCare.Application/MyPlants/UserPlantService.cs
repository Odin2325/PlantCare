using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Application.Care;
using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;

namespace PlantCare.Application.MyPlants;

internal sealed class UserPlantService(IUserPlantRepository userPlantRepository, IPlantSpeciesRepository plantSpeciesRepository, IUnitOfWork unitOfWork): IUserPlantService
{
    public async Task<IReadOnlyList<UserPlantDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);

        var userPlants =
            await userPlantRepository.GetAllForUserAsync(
                userId,
                cancellationToken);

        return userPlants
            .Select(MapToDto)
            .ToList();
    }

    public async Task<UserPlantDto?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);

        var userPlant = await userPlantRepository.GetByIdForUserAsync(
                id,
                userId,
                cancellationToken);

        return userPlant is null
            ? null
            : MapToDto(userPlant);
    }

    public async Task<UserPlantDto?> AddAsync(Guid userId, AddUserPlantCommand command, CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        ArgumentNullException.ThrowIfNull(command);

        var plantSpecies = await plantSpeciesRepository.GetByIdAsync(command.PlantSpeciesId, cancellationToken);

        if (plantSpecies is null)
        {
            return null;
        }

        var userPlant = UserPlant.Create(
            userId: userId,
            plantSpeciesId: plantSpecies.Id,
            nickname: command.Nickname,
            location: command.Location,
            acquiredOn: command.AcquiredOn,
            notes: command.Notes,
            createdAtUtc: DateTimeOffset.UtcNow);

        userPlant.AddCareSchedule(CareActionType.Watering, plantSpecies.DefaultWateringIntervalDays);

        if (plantSpecies.DefaultFertilizingIntervalDays is int fertilizingIntervalDays)
        {
            userPlant.AddCareSchedule(CareActionType.Fertilizing, fertilizingIntervalDays);
        }

        userPlantRepository.Add(userPlant);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserPlantDto(
            Id: userPlant.Id,
            PlantSpeciesId: plantSpecies.Id,
            SpeciesCommonName: plantSpecies.CommonName,
            SpeciesScientificName:
                plantSpecies.ScientificName,
            Nickname: userPlant.Nickname,
            Location: userPlant.Location,
            AcquiredOn: userPlant.AcquiredOn,
            Notes: userPlant.Notes,
            IsActive: userPlant.IsActive,
            CreatedAtUtc: userPlant.CreatedAtUtc,
            DefaultWateringIntervalDays:
                plantSpecies.DefaultWateringIntervalDays,
            DefaultFertilizingIntervalDays:
                plantSpecies.DefaultFertilizingIntervalDays,
            CareSchedules:
        userPlant.CareSchedules
        .OrderBy(schedule => schedule.ActionType)
        .Select(schedule => new CareScheduleDto(
            Id: schedule.Id,
            ActionType: schedule.ActionType,
            IntervalDays: schedule.IntervalDays,
            LastCompletedAtUtc: schedule.LastCompletedAtUtc,
            NextDueAtUtc: schedule.NextDueAtUtc,
            IsEnabled: schedule.IsEnabled))
        .ToList());
    }

    private static UserPlantDto MapToDto(UserPlant userPlant)
    {
        return new UserPlantDto(
            Id: userPlant.Id,
            PlantSpeciesId: userPlant.PlantSpeciesId,
            SpeciesCommonName:
                userPlant.PlantSpecies.CommonName,
            SpeciesScientificName:
                userPlant.PlantSpecies.ScientificName,
            Nickname: userPlant.Nickname,
            Location: userPlant.Location,
            AcquiredOn: userPlant.AcquiredOn,
            Notes: userPlant.Notes,
            IsActive: userPlant.IsActive,
            CreatedAtUtc: userPlant.CreatedAtUtc,
            DefaultWateringIntervalDays:
                userPlant.PlantSpecies
                    .DefaultWateringIntervalDays,
            DefaultFertilizingIntervalDays:
                userPlant.PlantSpecies
                    .DefaultFertilizingIntervalDays,
            CareSchedules:
                userPlant.CareSchedules
                .OrderBy(schedule => schedule.ActionType)
                .Select(schedule => new CareScheduleDto(
                    Id: schedule.Id,
                    ActionType: schedule.ActionType,
                    IntervalDays: schedule.IntervalDays,
                    LastCompletedAtUtc: schedule.LastCompletedAtUtc,
                    NextDueAtUtc: schedule.NextDueAtUtc,
                    IsEnabled: schedule.IsEnabled))
                .ToList());
    }

    private static void ValidateUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid user ID must be provided.",
                nameof(userId));
        }
    }
}