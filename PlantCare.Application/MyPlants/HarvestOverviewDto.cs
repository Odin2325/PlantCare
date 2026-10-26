namespace PlantCare.Application.MyPlants;

public sealed record HarvestOverviewRow(Guid Id, Guid UserPlantId, DateOnly HarvestedOn, decimal? Quantity, string? Unit, string? Notes);
// Fixed-scale strings retain decimal precision when filtered totals are combined by the browser.
public sealed record PlantHarvestSummary(Guid UserPlantId, int RecordCount, int UnquantifiedCount, IReadOnlyDictionary<string, string> Quantities);
public sealed record MonthlyPlantHarvestSummary(Guid UserPlantId, string Month, int RecordCount, int UnquantifiedCount, IReadOnlyDictionary<string, string> Quantities);
public sealed record HarvestOverviewDto(DateOnly From, DateOnly To, IReadOnlyList<HarvestOverviewRow> Rows, IReadOnlyList<PlantHarvestSummary> Plants, IReadOnlyList<MonthlyPlantHarvestSummary> Months);
