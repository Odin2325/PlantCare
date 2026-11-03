namespace PlantCare.Domain.Entities;

public sealed class PlantPhotoEntry
{
    private PlantPhotoEntry() { }
    public Guid Id { get; private set; }
    public Guid UserPlantId { get; private set; }
    public Guid UploadRequestId { get; private set; }
    public string UploadFingerprint { get; private set; } = "";
    public DateOnly PhotographedOn { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid Version { get; private set; }
    public UserPlant UserPlant { get; private set; } = null!;

    public static string? Validate(DateOnly date, string? notes, DateTimeOffset now)
    {
        notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (date == default || date > DateOnly.FromDateTime(now.UtcDateTime).AddDays(1))
            throw new ArgumentException("Choose a photo date no later than today in your time zone.");
        if (notes?.Length > 500) throw new ArgumentException("Photo notes must be at most 500 characters.");
        return notes;
    }
    public static PlantPhotoEntry Create(Guid plantId, Guid requestId, string fingerprint, DateOnly date, string? notes, DateTimeOffset now)
    {
        if (plantId == Guid.Empty || requestId == Guid.Empty || fingerprint.Length != 64)
            throw new ArgumentException("A plant, upload request and photo fingerprint are required.");
        return new() { Id = Guid.NewGuid(), UserPlantId = plantId, UploadRequestId = requestId,
            UploadFingerprint = fingerprint, PhotographedOn = date, Notes = Validate(date, notes, now), CreatedAtUtc = now, Version = Guid.NewGuid() };
    }
    public void Edit(DateOnly date, string? notes, DateTimeOffset now)
    {
        Notes = Validate(date, notes, now); PhotographedOn = date; Version = Guid.NewGuid();
    }
}
