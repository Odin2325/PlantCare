namespace PlantCare.Domain.Entities;

public sealed class HarvestRecord
{
    private HarvestRecord() { }
    public Guid Id { get; private set; }
    public Guid UserPlantId { get; private set; }
    public DateOnly HarvestedOn { get; private set; }
    public decimal? Quantity { get; private set; }
    public string? Unit { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }
    public UserPlant UserPlant { get; private set; } = null!;
    public static HarvestRecord Create(Guid plantId, DateOnly date, decimal? quantity, string? unit, string? notes, DateTimeOffset now)
    {
        unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (plantId == Guid.Empty || date == default)
            throw new ArgumentException("A plant and harvest date are required.");
        // A calendar day can already be tomorrow in a user's time zone.
        if (date > DateOnly.FromDateTime(now.UtcDateTime).AddDays(1))
            throw new ArgumentException("A harvest cannot be recorded in the future.");
        if (quantity.HasValue != (unit is not null) || quantity is <= 0 or > 1_000_000 ||
            (quantity.HasValue && decimal.Round(quantity.Value, 3) != quantity.Value))
            throw new ArgumentException("Use a positive quantity up to 1000000, with at most three decimal places and a unit.");
        if (unit is not null && unit is not ("items" or "g" or "kg"))
            throw new ArgumentException("Choose items, g or kg.");
        if (unit == "items" && quantity.HasValue && decimal.Truncate(quantity.Value) != quantity.Value)
            throw new ArgumentException("Use a whole number of items.");
        if (notes?.Length > 1000)
            throw new ArgumentException("Harvest notes must be at most 1000 characters.");
        return new() { Id = Guid.NewGuid(), UserPlantId = plantId, HarvestedOn = date, Quantity = quantity, Unit = unit, Notes = notes, RecordedAtUtc = now };
    }
}