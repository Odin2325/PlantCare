namespace PlantCare.Application.MyPlants;

public sealed record UpdateUserPlantCommand(
    string Nickname,
    string? Location,
    DateOnly? AcquiredOn,
    string? Notes);