using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PlantCare.Infrastructure.Operations;

namespace PlantCare.Api.Health;

public sealed class OperationsHealthCheck(IOptions<WorkerHealthOptions> options, TimeProvider time) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = options.Value;
            if (string.IsNullOrWhiteSpace(settings.HeartbeatPath) || settings.MaxWorkerAgeSeconds < 1 || settings.MinFreeBytes < 0)
                return Unhealthy();
            var state = JsonSerializer.Deserialize<WorkerHeartbeat>(File.ReadAllText(settings.HeartbeatPath));
            var age = state?.LastSuccessUtc is { } success ? time.GetUtcNow() - success : TimeSpan.MaxValue;
            if (state?.LastPassSucceeded != true || age < TimeSpan.Zero || age > TimeSpan.FromSeconds(settings.MaxWorkerAgeSeconds))
                return Unhealthy();
            var root = Path.GetPathRoot(Path.GetFullPath(settings.HeartbeatPath))!;
            if (new DriveInfo(root).AvailableFreeSpace < settings.MinFreeBytes) return Unhealthy();
            return Task.FromResult(HealthCheckResult.Healthy());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException)
        {
            return Unhealthy();
        }
    }

    private static Task<HealthCheckResult> Unhealthy() => Task.FromResult(HealthCheckResult.Unhealthy("Operations check failed."));
}
