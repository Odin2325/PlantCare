namespace PlantCare.Application.Dashboard;

public interface IDashboardService
{
    Task<IReadOnlyList<CareDueDto>> GetCareDueAsync(
        Guid userId,
        int daysAhead,
        CancellationToken cancellationToken = default);
}