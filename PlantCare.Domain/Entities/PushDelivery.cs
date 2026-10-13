namespace PlantCare.Domain.Entities;

public enum PushDeliveryStatus { Pending, Accepted, Expired, Failed, Skipped }

// Endpoint URLs and keys stay in PushSubscriptions, not this durable ledger.
public sealed class PushDelivery
{
    private PushDelivery() { }
    public Guid NotificationId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public PushDeliveryStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public int? LastStatusCode { get; private set; }
    public DateTimeOffset? NextAttemptAtUtc { get; private set; }
    public DateTimeOffset? FinishedAtUtc { get; private set; }

    public static PushDelivery Create(Guid notificationId, Guid subscriptionId, DateTimeOffset now) =>
        new() { NotificationId = notificationId, SubscriptionId = subscriptionId, NextAttemptAtUtc = now };

    public void RecordAttempt(DateTimeOffset now, int? statusCode, bool retry, TimeSpan? retryAfter = null)
    {
        AttemptCount++;
        LastStatusCode = statusCode;
        if (!retry)
        {
            Finish(statusCode is >= 200 and < 300 ? PushDeliveryStatus.Accepted :
                statusCode is 404 or 410 ? PushDeliveryStatus.Expired : PushDeliveryStatus.Failed, now);
            return;
        }
        if (AttemptCount >= 5) { Finish(PushDeliveryStatus.Failed, now); return; }
        var delay = TimeSpan.FromMinutes(AttemptCount switch { 1 => 1, 2 => 5, 3 => 15, _ => 60 });
        if (retryAfter > delay) delay = retryAfter.Value;
        NextAttemptAtUtc = now.Add(delay > TimeSpan.FromDays(1) ? TimeSpan.FromDays(1) : delay);
    }

    public void Skip(DateTimeOffset now) => Finish(PushDeliveryStatus.Skipped, now);

    private void Finish(PushDeliveryStatus status, DateTimeOffset now)
    {
        Status = status;
        NextAttemptAtUtc = null;
        FinishedAtUtc = now;
    }
}
