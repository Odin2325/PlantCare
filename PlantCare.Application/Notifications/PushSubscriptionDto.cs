namespace PlantCare.Application.Notifications;

public sealed record SavePushSubscriptionCommand(string Endpoint, string P256dh, string Auth);
public sealed record PushSubscriptionDto(Guid Id, string Endpoint, DateTimeOffset CreatedAtUtc);
