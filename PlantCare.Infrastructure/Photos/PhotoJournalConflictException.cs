namespace PlantCare.Infrastructure.Photos;

public sealed class PhotoJournalConflictException(string message) : Exception(message);
