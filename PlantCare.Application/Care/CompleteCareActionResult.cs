namespace PlantCare.Application.Care;

public sealed record CompleteCareActionResult(
    CareScheduleDto Schedule,
    CareEventDto Event);