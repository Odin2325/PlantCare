using Microsoft.Extensions.Options;
using PlantCare.Application.Notifications;
using PlantCare.Infrastructure.Operations;

namespace PlantCare.Worker;

public sealed class NotificationWorkerOptions
{
    public int PollIntervalSeconds { get; init; } = 60;
    public int BatchSize { get; init; } = 100;
}

public sealed class Worker(IServiceScopeFactory scopeFactory, IOptions<NotificationWorkerOptions> options, ILogger<Worker> logger, WorkerHeartbeatStore heartbeat) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        UpdateHeartbeat(heartbeat.Reset);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, settings.PollIntervalSeconds)));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var created = await service.GenerateDueAsync(settings.BatchSize, stoppingToken);
                if (created > 0) logger.LogInformation("Created {NotificationCount} care notifications.", created);
                var pushDelivery = scope.ServiceProvider.GetRequiredService<IPushDeliveryService>();
                var pushed = await pushDelivery.DeliverPendingAsync(settings.BatchSize, stoppingToken);
                if (pushed > 0) logger.LogInformation("Processed push delivery for {NotificationCount} notifications.", pushed);
                UpdateHeartbeat(heartbeat.Succeeded);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                UpdateHeartbeat(heartbeat.Failed);
                logger.LogError(exception, "Notification generation failed; the worker will retry.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private void UpdateHeartbeat(Action update)
    {
        try { update(); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Monitoring failure must not stop care processing. Missing/stale
            // heartbeat data still makes the operations endpoint unhealthy.
            logger.LogWarning("Worker heartbeat could not be updated.");
        }
    }
}
