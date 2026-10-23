namespace PlantCare.Application.Calendar;

public sealed record CalendarShareDto(Guid Id, string OwnerEmail, string RecipientEmail, DateTimeOffset CreatedAtUtc, bool IsOwnedByCurrentUser,
    bool? EmailNotificationSent = null, bool IsPendingInvitation = false, DateTimeOffset? ExpiresAtUtc = null);
public sealed record CreateCalendarShareCommand(string RecipientEmail);
