namespace PlantCare.Domain.Entities;

public sealed class PushSubscription
{
    private PushSubscription() { }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Endpoint { get; private set; } = string.Empty;
    public string P256dh { get; private set; } = string.Empty;
    public string Auth { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static PushSubscription Create(Guid userId, string endpoint, string p256dh, string auth, DateTimeOffset now)
    {
        Validate(userId, endpoint, p256dh, auth);
        return new() { Id = Guid.NewGuid(), UserId = userId, Endpoint = endpoint.Trim(), P256dh = p256dh.Trim(), Auth = auth.Trim(), CreatedAtUtc = now, UpdatedAtUtc = now };
    }
    public void Update(string p256dh, string auth, DateTimeOffset now)
    {
        Validate(UserId, Endpoint, p256dh, auth); P256dh = p256dh.Trim(); Auth = auth.Trim(); UpdatedAtUtc = now;
    }
    private static void Validate(Guid userId, string endpoint, string p256dh, string auth)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A valid user ID is required.");
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) throw new ArgumentException("A valid HTTPS push endpoint is required.", nameof(endpoint));
        if (string.IsNullOrWhiteSpace(p256dh) || string.IsNullOrWhiteSpace(auth)) throw new ArgumentException("Push encryption keys are required.");
    }
}
