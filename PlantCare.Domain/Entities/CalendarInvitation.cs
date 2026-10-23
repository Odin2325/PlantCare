namespace PlantCare.Domain.Entities;

public sealed class CalendarInvitation
{
    private CalendarInvitation() { }
    public Guid Id { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string RecipientEmail { get; private set; } = string.Empty;
    public string NormalizedRecipientEmail { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public Guid? AcceptedShareId { get; private set; }

    public static CalendarInvitation Create(Guid owner, string email, string tokenHash, DateTimeOffset now)
    {
        if (owner == Guid.Empty || string.IsNullOrWhiteSpace(email) || email.Length > 256 || tokenHash.Length != 64)
            throw new ArgumentException("Valid invitation values are required.");
        return new() { Id = Guid.NewGuid(), OwnerUserId = owner, RecipientEmail = email.Trim(),
            NormalizedRecipientEmail = email.Trim().ToUpperInvariant(), TokenHash = tokenHash,
            CreatedAtUtc = now, ExpiresAtUtc = now.AddDays(7) };
    }
    public void Revoke(DateTimeOffset now) => RevokedAtUtc ??= now;
    public void ReplaceToken(string hash, DateTimeOffset now)
    {
        if (RevokedAtUtc is not null || AcceptedAtUtc is not null || hash.Length != 64)
            throw new InvalidOperationException("Invitation cannot be resent.");
        TokenHash = hash; ExpiresAtUtc = now.AddDays(7);
    }
    public void Accept(Guid shareId, DateTimeOffset now) { AcceptedShareId = shareId; AcceptedAtUtc = now; }
}
