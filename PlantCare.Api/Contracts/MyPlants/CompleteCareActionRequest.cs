using PlantCare.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace PlantCare.Api.Contracts.MyPlants;

public sealed class CompleteCareActionRequest
{
    public DateTimeOffset? CompletedAtUtc { get; init; }

    [MaxLength(CareEvent.NotesMaxLength)]
    public string? Notes { get; init; }
}