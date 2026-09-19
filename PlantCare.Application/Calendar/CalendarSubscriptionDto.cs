namespace PlantCare.Application.Calendar;

public sealed record CalendarSubscriptionStatusDto(bool IsActive, DateTimeOffset? CreatedAtUtc);
public sealed record CalendarSubscriptionCreatedDto(string SubscriptionUrl, DateTimeOffset CreatedAtUtc);
