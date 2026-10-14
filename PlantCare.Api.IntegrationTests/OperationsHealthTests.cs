using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PlantCare.Api.Health;
using PlantCare.Infrastructure.Operations;

namespace PlantCare.Api.IntegrationTests;

public sealed class OperationsHealthTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "plantcare-health-" + Guid.NewGuid().ToString("N"));
    private readonly Clock clock = new();
    private string FilePath => Path.Combine(directory, "worker.json");
    private WorkerHealthOptions Settings(long minFree = 0) => new() { HeartbeatPath = FilePath, MaxWorkerAgeSeconds = 180, MinFreeBytes = minFree };
    private async Task<HealthStatus> Status(long minFree = 0) => (await new OperationsHealthCheck(Options.Create(Settings(minFree)), clock).CheckHealthAsync(new())).Status;

    [Fact]
    public async Task PublicOperationsFailureDoesNotChangeApiReadinessOrExposeDetails()
    {
        await using var factory = new Infrastructure.PlantCareApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health/operations");
        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
        Assert.Equal(System.Net.HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task CompletedPassExpiresWhenWorkerStopsAndRecoversAfterProgress()
    {
        var writer = new WorkerHeartbeatStore(Options.Create(Settings()), clock);
        Assert.Equal(HealthStatus.Unhealthy, await Status());
        writer.Reset();
        Assert.Equal(HealthStatus.Unhealthy, await Status());
        writer.Succeeded();
        Assert.Equal(HealthStatus.Healthy, await Status());
        clock.Now = clock.Now.AddSeconds(181);
        Assert.Equal(HealthStatus.Unhealthy, await Status());
        writer.Succeeded();
        Assert.Equal(HealthStatus.Healthy, await Status());
        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Fact]
    public async Task FailedPassAndRestartInvalidateRecentSuccess()
    {
        var writer = new WorkerHeartbeatStore(Options.Create(Settings()), clock);
        writer.Succeeded();
        writer.Failed();
        Assert.Equal(HealthStatus.Unhealthy, await Status());
        writer.Succeeded();
        new WorkerHeartbeatStore(Options.Create(Settings()), clock).Reset();
        Assert.Equal(HealthStatus.Unhealthy, await Status());
    }

    [Fact]
    public async Task InvalidFutureStateAndDiskPressureFailClosed()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, "invalid-json");
        Assert.Equal(HealthStatus.Unhealthy, await Status());
        File.WriteAllText(FilePath, JsonSerializer.Serialize(new WorkerHeartbeat(clock.Now.AddMinutes(1), true)));
        Assert.Equal(HealthStatus.Unhealthy, await Status());
        File.WriteAllText(FilePath, JsonSerializer.Serialize(new WorkerHeartbeat(null, true)));
        Assert.Equal(HealthStatus.Unhealthy, await Status());
        new WorkerHeartbeatStore(Options.Create(Settings()), clock).Succeeded();
        Assert.Equal(HealthStatus.Unhealthy, await Status(long.MaxValue));
        Assert.Equal(HealthStatus.Healthy, await Status());
    }

    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
