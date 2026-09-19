namespace PlantCare.Domain.Entities;

public sealed class CalendarSubscription
{
    private CalendarSubscription() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public static CalendarSubscription Create(Guid userId, string tokenHash, DateTimeOffset createdAtUtc)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A valid user ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new ArgumentException("A token hash is required.", nameof(tokenHash));
        return new CalendarSubscription { Id = Guid.NewGuid(), UserId = userId, TokenHash = tokenHash, CreatedAtUtc = createdAtUtc };
    }

    public void Revoke(DateTimeOffset revokedAtUtc) => RevokedAtUtc ??= revokedAtUtc;
}
