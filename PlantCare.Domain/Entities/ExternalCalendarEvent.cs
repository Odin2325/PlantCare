namespace PlantCare.Domain.Entities;

public sealed class ExternalCalendarEvent
{
    private ExternalCalendarEvent() { }

    public Guid Id { get; private set; }
    public Guid ConnectionId { get; private set; }
    public string PlantCareEntryId { get; private set; } = string.Empty;
    public string ExternalEventId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static ExternalCalendarEvent Create(
        Guid connectionId,
        string plantCareEntryId,
        string externalEventId,
        DateTimeOffset createdAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            ConnectionId = connectionId,
            PlantCareEntryId = plantCareEntryId,
            ExternalEventId = externalEventId,
            CreatedAtUtc = createdAtUtc
        };

    public void ReplaceExternalEventId(string externalEventId) =>
        ExternalEventId = externalEventId;
}
