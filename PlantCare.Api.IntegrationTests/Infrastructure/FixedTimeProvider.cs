namespace PlantCare.Api.IntegrationTests.Infrastructure;

internal sealed class FixedTimeProvider(
    DateTimeOffset utcNow)
    : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
