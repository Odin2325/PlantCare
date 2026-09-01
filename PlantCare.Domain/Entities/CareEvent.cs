namespace PlantCare.Domain.Entities;

public sealed class CareEvent
{
    public const int NotesMaxLength = 1_000;

    private CareEvent()
    {
    }

    public Guid Id { get; private set; }

    public Guid CareScheduleId { get; private set; }

    public DateTimeOffset CompletedAtUtc { get; private set; }

    public DateTimeOffset RecordedAtUtc { get; private set; }

    public string? Notes { get; private set; }

    public CareSchedule CareSchedule { get; private set; } = null!;

    public static CareEvent Create(
        Guid careScheduleId,
        DateTimeOffset completedAtUtc,
        DateTimeOffset recordedAtUtc,
        string? notes = null)
    {
        if (careScheduleId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid care schedule ID must be provided.",
                nameof(careScheduleId));
        }

        return new CareEvent
        {
            Id = Guid.NewGuid(),
            CareScheduleId = careScheduleId,
            CompletedAtUtc = completedAtUtc,
            RecordedAtUtc = recordedAtUtc,
            Notes = NormalizeOptionalNotes(notes)
        };
    }

    private static string? NormalizeOptionalNotes(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > NotesMaxLength)
        {
            throw new ArgumentException(
                $"Notes cannot exceed {NotesMaxLength} characters.",
                nameof(value));
        }

        return normalizedValue;
    }
}