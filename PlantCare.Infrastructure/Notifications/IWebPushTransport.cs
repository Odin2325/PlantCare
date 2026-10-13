using System.Net;
using PlantCare.Domain.Entities;

namespace PlantCare.Infrastructure.Notifications;

public sealed record PushSendResult(HttpStatusCode StatusCode, TimeSpan? RetryAfter = null);

public interface IWebPushTransport
{
    Task<PushSendResult> SendAsync(PushSubscription subscription, string payload, CancellationToken cancellationToken);
}
