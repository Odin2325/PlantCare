namespace PlantCare.Domain.Entities;

public sealed class ShortlistedPlant
{
    private ShortlistedPlant() { }
    public Guid UserId { get; private set; }
    public Guid PlantSpeciesId { get; private set; }
    public DateTimeOffset SavedAtUtc { get; private set; }

    public static ShortlistedPlant Create(Guid userId, Guid plantSpeciesId, DateTimeOffset savedAtUtc)
    {
        if (userId == Guid.Empty || plantSpeciesId == Guid.Empty)
            throw new ArgumentException("An account and catalog plant are required.");
        return new() { UserId = userId, PlantSpeciesId = plantSpeciesId, SavedAtUtc = savedAtUtc };
    }
}
