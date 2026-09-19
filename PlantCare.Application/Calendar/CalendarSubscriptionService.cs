using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Domain.Entities;

namespace PlantCare.Application.Calendar;

internal sealed class CalendarSubscriptionService(
    ICalendarSubscriptionRepository repository,
    ICalendarService calendarService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICalendarSubscriptionService
{
    public async Task<CalendarSubscriptionStatusDto> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var subscription = await repository.GetActiveForUserAsync(userId, cancellationToken);
        return new(subscription is not null, subscription?.CreatedAtUtc);
    }

    public async Task<CalendarSubscriptionCreatedDto> CreateAsync(Guid userId, string urlPrefix, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var existing = await repository.GetActiveForUserAsync(userId, cancellationToken);
        existing?.Revoke(now);

        var token = CreateToken();
        var subscription = CalendarSubscription.Create(userId, HashToken(token), now);
        repository.Add(subscription);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new($"{urlPrefix.TrimEnd('/')}/{token}.ics", now);
    }

    public async Task<bool> RevokeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var subscription = await repository.GetActiveForUserAsync(userId, cancellationToken);
        if (subscription is null) return false;
        subscription.Revoke(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string?> GetCalendarAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100) return null;
        var subscription = await repository.GetActiveByTokenHashAsync(HashToken(token), cancellationToken);
        if (subscription is null) return null;
        var now = timeProvider.GetUtcNow();
        var entries = await calendarService.GetEntriesAsync(subscription.UserId, now.AddDays(-30), now.AddDays(365), cancellationToken);
        return BuildCalendar(entries, now);
    }

    private static string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string BuildCalendar(IEnumerable<CalendarEntryDto> entries, DateTimeOffset generatedAt)
    {
        var lines = new List<string> { "BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:-//PlantCare//Care Calendar//EN", "CALSCALE:GREGORIAN", "METHOD:PUBLISH", "X-WR-CALNAME:PlantCare" };
        foreach (var entry in entries)
        {
            lines.Add("BEGIN:VEVENT");
            lines.Add($"UID:{entry.Id}@plantcare");
            lines.Add($"DTSTAMP:{FormatDate(generatedAt)}");
            lines.Add($"DTSTART:{FormatDate(entry.StartsAtUtc)}");
            lines.Add($"DTEND:{FormatDate(entry.StartsAtUtc.AddMinutes(30))}");
            lines.Add($"SUMMARY:{Escape($"{entry.ActionType} — {entry.PlantName}")}");
            lines.Add($"STATUS:{(entry.Kind == CalendarEntryKind.Completed ? "COMPLETED" : "CONFIRMED")}");
            lines.Add("END:VEVENT");
        }
        lines.Add("END:VCALENDAR");
        return string.Join("\r\n", lines) + "\r\n";
    }

    private static string FormatDate(DateTimeOffset value) => value.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r", string.Empty).Replace("\n", "\\n");
}
