namespace PlantCare.Domain.Entities;

public sealed class CalendarShare
{
    private CalendarShare() { }
    public Guid Id { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public static CalendarShare Create(Guid ownerUserId, Guid recipientUserId, DateTimeOffset createdAtUtc)
    {
        if (ownerUserId == Guid.Empty || recipientUserId == Guid.Empty) throw new ArgumentException("Valid user IDs are required.");
        if (ownerUserId == recipientUserId) throw new InvalidOperationException("A calendar cannot be shared with its owner.");
        return new() { Id = Guid.NewGuid(), OwnerUserId = ownerUserId, RecipientUserId = recipientUserId, CreatedAtUtc = createdAtUtc };
    }
    public void Revoke(DateTimeOffset revokedAtUtc) => RevokedAtUtc ??= revokedAtUtc;
}
