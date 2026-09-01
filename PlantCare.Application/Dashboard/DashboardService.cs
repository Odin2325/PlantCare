using PlantCare.Application.Abstractions.Persistence;

namespace PlantCare.Application.Dashboard;

public sealed class DashboardService(
    IUserPlantRepository userPlantRepository,
    TimeProvider timeProvider)
    : IDashboardService
{
    public async Task<IReadOnlyList<CareDueDto>>
        GetCareDueAsync(
            Guid userId,
            int daysAhead,
            CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid user ID must be provided.",
                nameof(userId));
        }

        if (daysAhead is < 0 or > 365)
        {
            throw new ArgumentOutOfRangeException(
                nameof(daysAhead),
                "Days ahead must be between 0 and 365.");
        }

        var now = timeProvider.GetUtcNow();

        var today = new DateTimeOffset(
                now.Year,
                now.Month,
                now.Day,
                0,
                0,
                0,
                TimeSpan.Zero);

        var end = today.AddDays(daysAhead + 1);

        var plants = await userPlantRepository.GetForDashboardAsync(userId, end, cancellationToken);

        var result = new List<CareDueDto>();

        foreach (var plant in plants)
        {
            foreach (var schedule in plant.CareSchedules)
            {
                if (!schedule.IsEnabled ||
                    schedule.NextDueAtUtc is null)
                {
                    continue;
                }

                var dueAt =
                    schedule.NextDueAtUtc.Value;

                if (dueAt >= end)
                {
                    continue;
                }

                var status =
                    dueAt < today
                        ? CareDueStatus.Overdue
                        : dueAt < today.AddDays(1)
                            ? CareDueStatus.DueToday
                            : CareDueStatus.Upcoming;

                result.Add(
                    new CareDueDto(
                        UserPlantId: plant.Id,
                        PlantName: plant.Nickname,
                        SpeciesCommonName:
                            plant.PlantSpecies.CommonName,
                        ActionType:
                            schedule.ActionType,
                        DueAtUtc:
                            dueAt,
                        Status:
                            status));
            }
        }

        return result
            .OrderBy(item => item.Status)
            .ThenBy(item => item.DueAtUtc)
            .ToList();
    }
}