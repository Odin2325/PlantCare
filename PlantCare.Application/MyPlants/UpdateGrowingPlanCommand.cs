namespace PlantCare.Application.MyPlants;

public sealed record UpdateGrowingPlanCommand(
    DateOnly? PlannedSowingOn, DateOnly? PlannedPlantingOutOn, DateOnly? ExpectedHarvestOn);
