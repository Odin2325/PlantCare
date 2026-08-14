namespace PlantCare.Application.MyPlants;

public sealed record AddUserPlantCommand(
    Guid PlantSpeciesId,
    string Nickname,
    string? Location,
    DateOnly? AcquiredOn,
    string? Notes);