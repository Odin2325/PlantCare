namespace PlantCare.Infrastructure.Identity;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public bool UseSsl { get; init; } = true;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "PlantCare";
    public string ClientBaseUrl { get; init; } = "http://localhost:4200";

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(Host) &&
        Port is > 0 and <= 65535 &&
        !string.IsNullOrWhiteSpace(FromAddress) &&
        Uri.TryCreate(
            ClientBaseUrl,
            UriKind.Absolute,
            out var clientBaseUri) &&
        clientBaseUri.Scheme is "http" or "https";
}
