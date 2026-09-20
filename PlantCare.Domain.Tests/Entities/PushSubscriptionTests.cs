using PlantCare.Domain.Entities;

namespace PlantCare.Domain.Tests.Entities;

public sealed class PushSubscriptionTests
{
    [Fact]
    public void Create_RequiresHttpsEndpoint()
    {
        Assert.Throws<ArgumentException>(() => PushSubscription.Create(Guid.NewGuid(), "http://example.com/push", "key", "auth", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Update_ReplacesKeysWithoutChangingEndpoint()
    {
        var subscription = PushSubscription.Create(Guid.NewGuid(), "https://example.com/push", "old-key", "old-auth", DateTimeOffset.UtcNow);
        subscription.Update("new-key", "new-auth", DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.Equal("https://example.com/push", subscription.Endpoint);
        Assert.Equal("new-key", subscription.P256dh);
    }
}
