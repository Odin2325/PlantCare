using PlantCare.Domain.Entities;

namespace PlantCare.Domain.Tests.Entities;

public sealed class CalendarShareTests
{
    [Fact]
    public void Create_RejectsSharingWithOwner()
    {
        var userId = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() => CalendarShare.Create(userId, userId, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Revoke_IsIdempotent()
    {
        var share = CalendarShare.Create(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var revokedAt = DateTimeOffset.UtcNow;
        share.Revoke(revokedAt);
        share.Revoke(revokedAt.AddMinutes(1));
        Assert.Equal(revokedAt, share.RevokedAtUtc);
    }
}
