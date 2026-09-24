using PlantCare.Application.Abstractions.Persistence;

namespace PlantCare.Application.Calendar;

internal sealed class CalendarService(ICalendarRepository repository) : ICalendarService
{
    public async Task<IReadOnlyList<CalendarEntryDto>> GetEntriesAsync(Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A valid user ID is required.", nameof(userId));
        if (toUtc <= fromUtc) throw new ArgumentException("The end of the range must be after the start.", nameof(toUtc));
        if (toUtc - fromUtc > TimeSpan.FromDays(366)) throw new ArgumentOutOfRangeException(nameof(toUtc), "The calendar range cannot exceed 366 days.");

        var schedules = await repository.GetSchedulesAsync(userId, toUtc, cancellationToken);
        var completedEvents = await repository.GetCompletedEventsAsync(userId, fromUtc, toUtc, cancellationToken);

        var entries = new List<CalendarEntryDto>();
        foreach (var schedule in schedules)
        {
            var occurrence = schedule.NextDueAtUtc!.Value;
            while (occurrence < fromUtc)
                occurrence = schedule.GetNextOccurrenceAfter(occurrence);
            while (occurrence < toUtc)
            {
                entries.Add(new CalendarEntryDto(
                    $"{schedule.Id:N}-scheduled-{occurrence.UtcTicks}", schedule.UserPlantId,
                    schedule.UserPlant.Nickname, schedule.ActionType, occurrence, CalendarEntryKind.Scheduled));
                occurrence = schedule.GetNextOccurrenceAfter(occurrence);
            }
        }

        entries.AddRange(completedEvents.Select(careEvent => new CalendarEntryDto(
            careEvent.Id.ToString("N"), careEvent.CareSchedule.UserPlantId,
            careEvent.CareSchedule.UserPlant.Nickname, careEvent.CareSchedule.ActionType,
            careEvent.CompletedAtUtc, CalendarEntryKind.Completed)));

        return entries.OrderBy(entry => entry.StartsAtUtc).ThenBy(entry => entry.PlantName).ToList();
    }
}
