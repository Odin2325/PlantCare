using PlantCare.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace PlantCare.Api.Contracts.MyPlants;

public sealed class AddUserPlantRequest
{
    public Guid PlantSpeciesId { get; init; }

    [Required]
    [MaxLength(UserPlant.NicknameMaxLength)]
    public string Nickname { get; init; } = string.Empty;

    [MaxLength(UserPlant.LocationMaxLength)]
    public string? Location { get; init; }

    public DateOnly? AcquiredOn { get; init; }

    [MaxLength(UserPlant.NotesMaxLength)]
    public string? Notes { get; init; }

    [MaxLength(UserPlant.MaximumTagCount)]
    public IReadOnlyList<string>? Tags { get; init; }

    [Range(1, 3_650)]
    public int? WateringIntervalDays { get; init; }

    public DateTimeOffset? LastWateredAtUtc { get; init; }

    [Range(1, 3_650)]
    public int? FertilizingIntervalDays { get; init; }

    public DateTimeOffset? LastFertilizedAtUtc { get; init; }
}
