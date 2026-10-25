using System.Text.RegularExpressions;

namespace PlantCare.Domain.ValueObjects;

public sealed record PlantCatalogPhoto(
    string Path, string AltText, string Credit, string SourceUrl,
    string LicenseName, string LicenseUrl, string? Title = null);

public sealed record PlantCatalogProfile(
    string CareSourceUrl, DateOnly ReviewedOn, PlantCatalogPhoto? Photo,
    IReadOnlyList<PlantGrowthReference>? GrowthReferences = null,
    IReadOnlyList<string>? CommonNameAliases = null,
    string? NonToxicToCatsAndDogsSourceUrl = null,
    IReadOnlyList<string>? Categories = null,
    string? CultivarName = null,
    string? GrowingNotes = null,
    string? HarvestInstructions = null,
    bool PhotoIsSpeciesReference = false)
{
    public void Validate()
    {
        Https(CareSourceUrl);
        if (NonToxicToCatsAndDogsSourceUrl is not null) Https(NonToxicToCatsAndDogsSourceUrl);
        if (ReviewedOn == default) throw new ArgumentException("A care review date is required.");
        if (Categories is { } categories)
        {
            string[] allowed = ["houseplant", "fruit", "vegetable", "root-tuber", "herb", "tree"];
            if (categories.Count is < 1 or > 6 || categories.Distinct().Count() != categories.Count ||
                categories.Any(category => !allowed.Contains(category)))
                throw new ArgumentException("Use distinct supported catalog categories.");
        }
        if (CultivarName is not null) Required(CultivarName, 80);
        if (GrowingNotes is not null) Required(GrowingNotes, 1_000);
        if (HarvestInstructions is not null) Required(HarvestInstructions, 1_000);
        if (PhotoIsSpeciesReference && (Photo is null || CultivarName is null))
            throw new ArgumentException("A cultivar and photo are required for a species-reference photo label.");
        if (CommonNameAliases is { } aliases)
        {
            if (aliases.Count > 8) throw new ArgumentException("At most eight common-name aliases are supported.");
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var alias in aliases)
                if (string.IsNullOrWhiteSpace(alias) || alias.Length > 80 || alias != alias.Trim() || !names.Add(alias))
                    throw new ArgumentException("Common-name aliases must be distinct, trimmed names of at most 80 characters.");
        }
        if (Photo is { } photo) ValidatePhoto(photo);
        if (GrowthReferences is null) return;
        if (GrowthReferences.Count > 6) throw new ArgumentException("At most six growth references are supported.");
        foreach (var reference in GrowthReferences)
        {
            if (reference is null || reference.Photo is null)
                throw new ArgumentException("A growth reference and photo are required.");
            Required(reference.Title, 100); Required(reference.Description, 600);
            ValidatePhoto(reference.Photo);
        }
    }

    private static void ValidatePhoto(PlantCatalogPhoto photo)
    {
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

public sealed record PlantGrowthReference(string Title, string Description, PlantCatalogPhoto Photo);
