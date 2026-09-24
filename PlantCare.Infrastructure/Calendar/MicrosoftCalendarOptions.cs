namespace PlantCare.Infrastructure.Calendar;

public sealed class MicrosoftCalendarOptions
{
    public const string SectionName = "MicrosoftCalendar";

    public bool Enabled { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string CallbackUrl { get; init; } = string.Empty;
    public string Tenant { get; init; } = "common";

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret) &&
        !string.IsNullOrWhiteSpace(Tenant) &&
        Uri.TryCreate(CallbackUrl, UriKind.Absolute, out _);
}
