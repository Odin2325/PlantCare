using Microsoft.Extensions.Options;
using PlantCare.Application.Notifications;

namespace PlantCare.Worker;

public sealed class NotificationWorkerOptions
{
    public int PollIntervalSeconds { get; init; } = 60;
    public int BatchSize { get; init; } = 100;
}

public sealed class Worker(IServiceScopeFactory scopeFactory, IOptions<NotificationWorkerOptions> options, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
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
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Notification generation failed; the worker will retry."); }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
