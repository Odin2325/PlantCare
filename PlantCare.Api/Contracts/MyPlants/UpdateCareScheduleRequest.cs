using System.ComponentModel.DataAnnotations;

namespace PlantCare.Api.Contracts.MyPlants;

public sealed class UpdateCareScheduleRequest
{
    [Range(1, 3_650)]
    public int IntervalDays { get; init; }

    public bool IsEnabled { get; init; }
}
