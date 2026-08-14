using System.ComponentModel.DataAnnotations;
using PlantCare.Domain.Entities;

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
}