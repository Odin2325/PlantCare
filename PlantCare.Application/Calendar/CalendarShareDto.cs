namespace PlantCare.Application.Calendar;

public sealed record CalendarShareDto(Guid Id, string OwnerEmail, string RecipientEmail, DateTimeOffset CreatedAtUtc, bool IsOwnedByCurrentUser);
public sealed record CreateCalendarShareCommand(string RecipientEmail);
