using System.ComponentModel.DataAnnotations;
using PlantCare.Domain.Enums;

namespace PlantCare.Api.Contracts.MyPlants;

public sealed class UpdateCareScheduleRequest
{
    [Range(1, 3_650)]
    public int IntervalDays { get; init; }

    public bool IsEnabled { get; init; }

    public CareScheduleMode ScheduleMode { get; init; }

    [Range(0, 127)]
    public int WeekDays { get; init; }

    public TimeOnly? PreferredTimeLocal { get; init; }

    [MaxLength(100)]
    public string? TimeZoneId { get; init; }
}
