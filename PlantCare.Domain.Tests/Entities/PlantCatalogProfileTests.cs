using System.Text.Json;
using PlantCare.Domain.ValueObjects;

namespace PlantCare.Domain.Tests.Entities;

public sealed class PlantCatalogProfileTests
{
    private static readonly PlantCatalogPhoto Photo = new("/images/plants/example.jpg", "Leaves", "Photographer",
        "https://example.org/photo", "CC BY 4.0", "https://creativecommons.org/licenses/by/4.0/");

    [Fact]
    public void ExistingProfilesWithoutGrowthReferencesRemainReadable()
    {
        var profile = JsonSerializer.Deserialize<PlantCatalogProfile>("""
            {"CareSourceUrl":"https://example.org/care","ReviewedOn":"2026-10-22","Photo":null}
            """)!;
        profile.Validate();
        Assert.Null(profile.GrowthReferences);
        Assert.Null(profile.CommonNameAliases);
    }

    [Fact]
    public void AliasesAreOptionalButMustBeBoundedDistinctNames()
    {
        var profile = new PlantCatalogProfile("https://example.org/care", new(2026, 10, 22), null,
            CommonNameAliases: ["Devil's ivy", "Pothos"]);
        profile.Validate();
        foreach (var invalid in new IReadOnlyList<string>[] {
            [""], [" name"], ["Name "], [null!], [new string('a', 81)], ["Pothos", "pothos"],
            Enumerable.Range(0, 9).Select(i => "Name " + i).ToArray() })
            Assert.Throws<ArgumentException>(() => (profile with { CommonNameAliases = invalid }).Validate());
        var roundTrip = JsonSerializer.Deserialize<PlantCatalogProfile>(JsonSerializer.Serialize(profile))!;
        Assert.Equal(profile.CommonNameAliases, roundTrip.CommonNameAliases);
    }

    [Fact]
    public void GrowthPhotosAreValidatedEvenWithoutACoverPhoto()
    {
        var reference = new PlantGrowthReference("Roots", "Root beginnings on a plantlet.", Photo);
        var profile = new PlantCatalogProfile("https://example.org/care", new(2026, 10, 22), null, [reference]);
        profile.Validate();
        foreach (var invalid in new[] {
            reference with { Photo = Photo with { Path = "https://example.org/tracker.jpg" } },
            reference with { Photo = Photo with { LicenseName = "CC BY-NC 4.0" } },
            reference with { Title = " " }, reference with { Description = new string('a', 601) },
            reference with { Photo = null! }, null! })
            Assert.Throws<ArgumentException>(() => (profile with { GrowthReferences = [invalid] }).Validate());
        Assert.Throws<ArgumentException>(() => (profile with {
            GrowthReferences = Enumerable.Repeat(reference, 7).ToArray() }).Validate());
    }
}
