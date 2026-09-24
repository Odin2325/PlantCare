using Microsoft.Extensions.Options;
using PlantCare.Application.Calendar;

namespace PlantCare.Worker;

public sealed class ExternalCalendarSyncOptions
{
    public bool Enabled { get; init; } = true;
    public int PollIntervalMinutes { get; init; } = 15;
    public int SyncIntervalHours { get; init; } = 6;
    public int BatchSize { get; init; } = 25;
}

public sealed class ExternalCalendarSyncWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<ExternalCalendarSyncOptions> options,
    ILogger<ExternalCalendarSyncWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation(
                "Automatic external-calendar synchronization is disabled.");
            return;
        }

        var pollInterval = TimeSpan.FromMinutes(
            Math.Max(1, settings.PollIntervalMinutes));
        var syncInterval = TimeSpan.FromHours(
            Math.Max(1, settings.SyncIntervalHours));
        var batchSize = Math.Max(1, settings.BatchSize);
        using var timer = new PeriodicTimer(pollInterval);

        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<
                    IAutomaticExternalCalendarSyncService>();
                var result = await service.SyncDueAsync(
                    syncInterval,
                    batchSize,
                    stoppingToken);

                if (result.Attempted > 0)
                {
                    logger.LogInformation(
                        "Automatic calendar synchronization processed {Attempted} connections: {Succeeded} succeeded and {Failed} failed.",
                        result.Attempted,
                        result.Succeeded,
                        result.Failed);
                }
            }
            catch (OperationCanceledException) when (
                stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Automatic calendar synchronization failed; the worker will retry.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
