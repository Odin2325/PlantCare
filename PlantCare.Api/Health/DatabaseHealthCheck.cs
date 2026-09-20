using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PlantCare.Infrastructure.Persistence;

namespace PlantCare.Api.Health;

public sealed class DatabaseHealthCheck(PlantCareDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("The database is reachable.")
                : HealthCheckResult.Unhealthy("The database is unreachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("The database health check failed.", exception);
        }
    }
}
