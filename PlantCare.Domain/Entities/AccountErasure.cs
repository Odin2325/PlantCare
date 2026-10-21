namespace PlantCare.Domain.Entities;

// No account FK: this minimal marker must survive account removal and recovery.
public sealed class AccountErasure
{
    private AccountErasure() { }
    public Guid UserId { get; private set; }
    public DateTimeOffset ErasedAtUtc { get; private set; }
    public static AccountErasure Create(Guid userId, DateTimeOffset atUtc)
    {
        if (userId == Guid.Empty) throw new ArgumentException("An erasure marker requires a user identifier.");
        return new() { UserId = userId, ErasedAtUtc = atUtc.ToUniversalTime() };
    }
}
