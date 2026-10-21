using System.Text.RegularExpressions;

namespace PlantCare.Domain.ValueObjects;

public sealed record PlantCatalogPhoto(
    string Path, string AltText, string Credit, string SourceUrl,
    string LicenseName, string LicenseUrl, string? Title = null);

public sealed record PlantCatalogProfile(
    string CareSourceUrl, DateOnly ReviewedOn, PlantCatalogPhoto? Photo)
{
    public void Validate()
    {
        Https(CareSourceUrl);
        if (ReviewedOn == default) throw new ArgumentException("A care review date is required.");
        if (Photo is not { } photo) return;
        if (photo.Path is null || !Regex.IsMatch(photo.Path, @"\A/images/plants/[a-z0-9-]+\.(jpg|jpeg|png|webp)\z"))
            throw new ArgumentException("Use a local plant image path.");
        Required(photo.AltText, 300); Required(photo.Credit, 200);
        if (photo.Title is not null) Required(photo.Title, 300);
        Required(photo.LicenseName, 80); Https(photo.SourceUrl); Https(photo.LicenseUrl);
        var licenses = new Dictionary<string, string>
        {
            ["CC BY 2.0"] = "https://creativecommons.org/licenses/by/2.0/",
            ["CC BY 3.0"] = "https://creativecommons.org/licenses/by/3.0/",
            ["CC BY 4.0"] = "https://creativecommons.org/licenses/by/4.0/",
            ["CC BY-SA 2.0"] = "https://creativecommons.org/licenses/by-sa/2.0/",
            ["CC BY-SA 3.0"] = "https://creativecommons.org/licenses/by-sa/3.0/",
            ["CC BY-SA 4.0"] = "https://creativecommons.org/licenses/by-sa/4.0/",
            ["CC0 1.0"] = "https://creativecommons.org/publicdomain/zero/1.0/"
        };
        if (!licenses.TryGetValue(photo.LicenseName, out var expected) || photo.LicenseUrl != expected)
            throw new ArgumentException("A supported, matching image license is required.");
    }

    private static void Required(string? value, int maximum)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximum)
            throw new ArgumentException("Catalog credit text is missing or too long.");
    }
    private static void Https(string? value)
    {
        if (value is null || value.Length > 1_000 || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("Catalog sources must use an absolute HTTPS URL without credentials.");
    }
}
