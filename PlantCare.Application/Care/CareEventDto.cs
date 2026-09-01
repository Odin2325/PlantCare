namespace PlantCare.Application.Care;

public sealed record CareEventDto(
    Guid Id,
    Guid CareScheduleId,
    DateTimeOffset CompletedAtUtc,
    DateTimeOffset RecordedAtUtc,
    string? Notes);