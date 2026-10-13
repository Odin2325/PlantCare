using Microsoft.Extensions.Options;
using WebPush;
using PushSubscription = PlantCare.Domain.Entities.PushSubscription;

namespace PlantCare.Infrastructure.Notifications;

internal sealed class WebPushTransport(HttpClient httpClient, IOptions<WebPushOptions> options, TimeProvider timeProvider)
    : IWebPushTransport
{
    public async Task<PushSendResult> SendAsync(PushSubscription subscription, string payload, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var client = new WebPushClient(httpClient);
        using var request = client.GenerateRequestDetails(
            new WebPush.PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth), payload,
            new Dictionary<string, object>
            {
                ["vapidDetails"] = new VapidDetails(settings.Subject, settings.PublicKey, settings.PrivateKey),
                ["TTL"] = 3600
            });
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var retryAfter = response.Headers.RetryAfter;
        return new(response.StatusCode, retryAfter?.Delta ??
            (retryAfter?.Date is { } date ? date - timeProvider.GetUtcNow() : null));
    }
}
