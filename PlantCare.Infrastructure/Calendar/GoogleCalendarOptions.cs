namespace PlantCare.Infrastructure.Calendar;

public sealed class GoogleCalendarOptions
{
    public const string SectionName = "GoogleCalendar";

    public bool Enabled { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string CallbackUrl { get; init; } = string.Empty;

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret) &&
        Uri.TryCreate(CallbackUrl, UriKind.Absolute, out _);
}
