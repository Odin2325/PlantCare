using System.Text.Json;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlantCare.Application.Notifications;
using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;
using PlantCare.Infrastructure.Persistence;

namespace PlantCare.Infrastructure.Notifications;

internal sealed class WebPushDeliveryService(PlantCareDbContext dbContext, IOptions<WebPushOptions> options,
    IWebPushTransport transport, TimeProvider timeProvider, ILogger<WebPushDeliveryService> logger) : IPushDeliveryService
{
    public async Task<int> DeliverPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        if (batchSize is < 1 or > 1_000) throw new ArgumentOutOfRangeException(nameof(batchSize));
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Subject) || string.IsNullOrWhiteSpace(settings.PublicKey) || string.IsNullOrWhiteSpace(settings.PrivateKey)) return 0;
        var now = timeProvider.GetUtcNow();
        var notifications = await dbContext.Notifications.Include(item => item.CareSchedule).ThenInclude(schedule => schedule.UserPlant)
            .ThenInclude(plant => plant.PlantSpecies)
            .Where(item => item.PushSentAtUtc == null && (item.PushNextAttemptAtUtc == null || item.PushNextAttemptAtUtc <= now))
            .OrderBy(item => item.PushNextAttemptAtUtc ?? item.CreatedAtUtc).ThenBy(item => item.Id)
            .Take(batchSize).ToListAsync(cancellationToken);
        var processed = 0;
        var passTime = Stopwatch.StartNew();
        foreach (var notification in notifications)
        {
            if (passTime.Elapsed >= TimeSpan.FromMinutes(1)) break;
            cancellationToken.ThrowIfCancellationRequested();
            now = timeProvider.GetUtcNow();
            var schedule = notification.CareSchedule;
            var preference = await dbContext.NotificationPreferences.AsNoTracking()
                .SingleOrDefaultAsync(item => item.UserId == notification.UserId, cancellationToken);
            var deliveries = await dbContext.PushDeliveries.Where(item => item.NotificationId == notification.Id).ToListAsync(cancellationToken);
            if (!schedule.IsEnabled || schedule.IsArchived || !schedule.UserPlant.IsActive ||
                schedule.NextDueAtUtc != notification.DueAtUtc || notification.CreatedAtUtc.AddDays(1) <= now ||
                (preference is not null && (!preference.PushEnabled || !preference.IsActionEnabled(schedule.ActionType))))
            {
                foreach (var delivery in deliveries.Where(item => item.Status == PushDeliveryStatus.Pending)) delivery.Skip(now);
                notification.MarkPushSent(now);
                await dbContext.SaveChangesAsync(cancellationToken);
                processed++;
                continue;
            }

            var subscriptions = await dbContext.PushSubscriptions.Where(item => item.UserId == notification.UserId).ToDictionaryAsync(item => item.Id, cancellationToken);
            if (deliveries.Count == 0)
            {
                deliveries = subscriptions.Keys.Select(id => PushDelivery.Create(notification.Id, id, now)).ToList();
                dbContext.PushDeliveries.AddRange(deliveries);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            // Angular reads NotificationOptions directly from notification, not
            // from an options child. Nesting them drops body, tag and tap action.
            var plantName = string.IsNullOrWhiteSpace(schedule.UserPlant.Nickname)
                ? schedule.UserPlant.PlantSpecies.CommonName : schedule.UserPlant.Nickname;
            var body = $"{plantName} needs care.";
            var payload = JsonSerializer.Serialize(new { notification = new { title = $"{schedule.ActionType} reminder",
                body, icon = GetReminderIcon(schedule.ActionType),
                tag = $"plantcare-{notification.Id:N}",
                data = new {
                    onActionClick = new { @default = new { operation = "openWindow", url = "/dashboard" } }
                }
            } });
            var attempts = 0;
            foreach (var delivery in deliveries.Where(item => item.Status == PushDeliveryStatus.Pending))
            {
                if (attempts >= 16 || passTime.Elapsed >= TimeSpan.FromMinutes(1)) break;
                cancellationToken.ThrowIfCancellationRequested();
                now = timeProvider.GetUtcNow();
                if (!subscriptions.TryGetValue(delivery.SubscriptionId, out var subscription)) delivery.Skip(now);
                else if (delivery.NextAttemptAtUtc > now) continue;
                else
                {
                    attempts++;
                    try
                    {
                        var result = await transport.SendAsync(subscription, payload, cancellationToken);
                        var code = (int)result.StatusCode;
                        delivery.RecordAttempt(timeProvider.GetUtcNow(), code, code is 408 or 429 || code >= 500, result.RetryAfter);
                        if (code is 404 or 410) dbContext.PushSubscriptions.Remove(subscription);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                    catch (Exception exception)
                    {
                        delivery.RecordAttempt(timeProvider.GetUtcNow(), null,
                            exception is not (ArgumentException or FormatException));
                        // Provider exceptions can contain endpoint URLs and keys.
                        logger.LogWarning("Push attempt failed for notification {NotificationId}, device {SubscriptionId}: {FailureType}.",
                            notification.Id, subscription.Id, exception.GetType().Name);
                    }
                    if (delivery.Status == PushDeliveryStatus.Failed)
                        logger.LogWarning("Push delivery stopped for notification {NotificationId}, device {SubscriptionId}, status {StatusCode}, attempts {AttemptCount}.",
                            notification.Id, subscription.Id, delivery.LastStatusCode, delivery.AttemptCount);
                }
                // Persist each device result before another send; ordinary
                // restarts must not resend to devices that already accepted it.
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            var pending = deliveries.Where(item => item.Status == PushDeliveryStatus.Pending).ToArray();
            if (pending.Length == 0) { notification.MarkPushSent(timeProvider.GetUtcNow()); processed++; }
            else
            {
                var next = pending.Min(item => item.NextAttemptAtUtc)!.Value;
                var current = timeProvider.GetUtcNow();
                notification.DeferPush(next > current ? next : current.AddMinutes(1));
            }
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return processed;
    }

    private static string GetReminderIcon(CareActionType action) => action switch
    {
        CareActionType.Watering => "/icons/care-watering.png",
        CareActionType.Fertilizing => "/icons/care-fertilizing.png",
        CareActionType.Misting => "/icons/care-misting.png",
        CareActionType.Pruning => "/icons/care-pruning.png",
        CareActionType.Repotting => "/icons/care-repotting.png",
        _ => "/icons/plantcare-192.png"
    };
}
