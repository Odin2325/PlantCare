using System.ComponentModel.DataAnnotations;
using PlantCare.Domain.Entities;

namespace PlantCare.Api.Contracts.MyPlants;

public sealed class CompleteCareActionRequest
{
    public DateTimeOffset? CompletedAtUtc { get; init; }

    [MaxLength(CareEvent.NotesMaxLength)]
    public string? Notes { get; init; }
}