using Microsoft.AspNetCore.Identity;

namespace PlantCare.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
}