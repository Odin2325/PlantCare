using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlantCare.Application.Notifications;
using PlantCare.Infrastructure.Persistence;
using WebPush;

namespace PlantCare.Infrastructure.Notifications;

internal sealed class WebPushDeliveryService(PlantCareDbContext dbContext, IOptions<WebPushOptions> options, TimeProvider timeProvider, ILogger<WebPushDeliveryService> logger) : IPushDeliveryService
{
    public async Task<int> DeliverPendingAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Subject) || string.IsNullOrWhiteSpace(settings.PublicKey) || string.IsNullOrWhiteSpace(settings.PrivateKey)) return 0;
        var notifications = await dbContext.Notifications.Include(item => item.CareSchedule).ThenInclude(schedule => schedule.UserPlant)
            .Where(item => item.PushSentAtUtc == null).OrderBy(item => item.CreatedAtUtc).Take(batchSize).ToListAsync(cancellationToken);
        using var client = new WebPushClient(); var vapid = new VapidDetails(settings.Subject, settings.PublicKey, settings.PrivateKey); var delivered = 0;
        foreach (var notification in notifications)
        {
            var subscriptions = await dbContext.PushSubscriptions.Where(item => item.UserId == notification.UserId).ToListAsync(cancellationToken); var retryNeeded = false;
            foreach (var item in subscriptions)
            {
                var payload = JsonSerializer.Serialize(new { notification = new { title = $"{notification.CareSchedule.ActionType} reminder", options = new { body = $"{notification.CareSchedule.UserPlant.Nickname} needs care.", icon = "/plantcare-icon.svg", data = new { onActionClick = new { @default = new { operation = "navigateLastFocusedOrOpen", url = "/notifications" } } } } } });
                try { await client.SendNotificationAsync(new WebPush.PushSubscription(item.Endpoint, item.P256dh, item.Auth), payload, vapid, cancellationToken); }
                catch (WebPushException exception) when (exception.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound) { dbContext.PushSubscriptions.Remove(item); }
                catch (Exception exception) { retryNeeded = true; logger.LogWarning(exception, "Push delivery failed for notification {NotificationId}.", notification.Id); }
            }
            if (!retryNeeded) { notification.MarkPushSent(timeProvider.GetUtcNow()); delivered++; }
        }
        if (notifications.Count > 0) await dbContext.SaveChangesAsync(cancellationToken);
        return delivered;
    }
}
