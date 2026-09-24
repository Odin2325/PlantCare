using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Application.Calendar;

namespace PlantCare.Infrastructure.Calendar;

internal sealed class AutomaticExternalCalendarSyncService(
    IExternalCalendarConnectionRepository repository,
    IExternalCalendarService googleCalendarService,
    IMicrosoftCalendarService microsoftCalendarService,
    IOptions<GoogleCalendarOptions> googleOptions,
    IOptions<MicrosoftCalendarOptions> microsoftOptions,
    TimeProvider timeProvider,
    ILogger<AutomaticExternalCalendarSyncService> logger)
    : IAutomaticExternalCalendarSyncService
{
    public async Task<AutomaticExternalCalendarSyncResultDto> SyncDueAsync(
        TimeSpan minimumSyncInterval,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (minimumSyncInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(minimumSyncInterval));
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize));

        var configuredProviders = new List<string>(2);
        if (googleOptions.Value.Enabled && googleOptions.Value.IsComplete)
            configuredProviders.Add("Google");
        if (microsoftOptions.Value.Enabled && microsoftOptions.Value.IsComplete)
            configuredProviders.Add("Microsoft");

        if (configuredProviders.Count == 0)
            return new AutomaticExternalCalendarSyncResultDto(0, 0, 0);

        var now = timeProvider.GetUtcNow();
        var connections = await repository.GetDueForSyncAsync(
            configuredProviders,
            now.Subtract(minimumSyncInterval),
            batchSize,
            cancellationToken);
        var succeeded = 0;
        var failed = 0;

        foreach (var connection in connections)
        {
            try
            {
                if (connection.Provider == "Google")
                {
                    await googleCalendarService.SyncGoogleAsync(
                        connection.UserId,
                        cancellationToken);
                }
                else if (connection.Provider == "Microsoft")
                {
                    await microsoftCalendarService.SyncAsync(
                        connection.UserId,
                        cancellationToken);
                }
                else
                {
                    continue;
                }

                succeeded++;
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failed++;
                logger.LogWarning(
                    exception,
                    "Automatic {Provider} calendar synchronization failed for user {UserId}.",
                    connection.Provider,
                    connection.UserId);
            }
        }

        return new AutomaticExternalCalendarSyncResultDto(
            connections.Count,
            succeeded,
            failed);
    }
}
