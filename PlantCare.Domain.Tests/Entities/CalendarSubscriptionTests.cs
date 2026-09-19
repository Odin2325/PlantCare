using PlantCare.Domain.Entities;

namespace PlantCare.Domain.Tests.Entities;

public sealed class CalendarSubscriptionTests
{
    [Fact]
    public void Create_StoresOnlyProvidedTokenHashAndStartsActive()
    {
        var subscription = CalendarSubscription.Create(Guid.NewGuid(), new string('A', 64), DateTimeOffset.UtcNow);

        Assert.Equal(new string('A', 64), subscription.TokenHash);
        Assert.Null(subscription.RevokedAtUtc);
    }

    [Fact]
    public void Revoke_IsIdempotent()
    {
        var subscription = CalendarSubscription.Create(Guid.NewGuid(), new string('A', 64), DateTimeOffset.UtcNow);
        var revokedAt = DateTimeOffset.UtcNow;

        subscription.Revoke(revokedAt);
        subscription.Revoke(revokedAt.AddMinutes(1));

        Assert.Equal(revokedAt, subscription.RevokedAtUtc);
    }
}
