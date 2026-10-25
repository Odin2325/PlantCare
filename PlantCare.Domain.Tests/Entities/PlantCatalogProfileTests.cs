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
        Assert.Null(profile.NonToxicToCatsAndDogsSourceUrl);
        Assert.Null(profile.Categories);
        Assert.Null(profile.GrowingNotes);
        Assert.Null(profile.HarvestInstructions);
        Assert.False(profile.PhotoIsSpeciesReference);
    }

    [Fact]
    public void GardenCategoriesOverlapButRejectUnknownDuplicateAndEmptyValues()
    {
        var profile = new PlantCatalogProfile("https://example.org/care", new(2026, 10, 25), Photo,
            Categories: ["fruit", "tree"], GrowingNotes: "Choose a suitable rootstock.",
            HarvestInstructions: "Pick when ripe.");
        profile.Validate();
        foreach (var invalid in new IReadOnlyList<string>[] { [], ["fruit", "fruit"], ["Fruit"], [null!], ["edible"], [" tree"] })
            Assert.Throws<ArgumentException>(() => (profile with { Categories = invalid }).Validate());
        var result = JsonSerializer.Deserialize<PlantCatalogProfile>(JsonSerializer.Serialize(profile))!;
        Assert.Equal(profile.Categories, result.Categories);
        Assert.Equal(profile.HarvestInstructions, result.HarvestInstructions);
    }

    [Fact]
    public void CultivarAndGardenGuidanceAreOptionalButBounded()
    {
        var profile = new PlantCatalogProfile("https://example.org/care", new(2026, 10, 25), Photo,
            CultivarName: "Tiny Tim", GrowingNotes: "A dwarf bush tomato.", HarvestInstructions: "Pick ripe fruit.",
            PhotoIsSpeciesReference: true);
        profile.Validate();
        Assert.Throws<ArgumentException>(() => (profile with { Photo = null }).Validate());
        Assert.Throws<ArgumentException>(() => (profile with { CultivarName = null }).Validate());
        foreach (var invalid in new[] { "", " ", new string('a', 81) })
            Assert.Throws<ArgumentException>(() => (profile with { CultivarName = invalid }).Validate());
        foreach (var invalid in new[] { "", " ", new string('a', 1001) })
        {
            Assert.Throws<ArgumentException>(() => (profile with { GrowingNotes = invalid }).Validate());
            Assert.Throws<ArgumentException>(() => (profile with { HarvestInstructions = invalid }).Validate());
        }
    }

    [Fact]
    public void NonToxicClassificationNeedsAValidSourceAndCannotContradictAToxicPlant()
    {
        var profile = new PlantCatalogProfile("https://example.org/care", new(2026, 10, 24), null,
            NonToxicToCatsAndDogsSourceUrl: "https://example.org/cats-and-dogs");
        profile.Validate();
        foreach (var source in new[] { "", "javascript:alert(1)", "http://example.org", "https://user:secret@example.org" })
            Assert.Throws<ArgumentException>(() => (profile with { NonToxicToCatsAndDogsSourceUrl = source }).Validate());

        var plant = PlantCare.Domain.Entities.PlantSpecies.Create("Example", "Example species", "Example plant",
            PlantCare.Domain.Enums.SunlightRequirement.BrightIndirectLight, "Indirect light", 7, "Check moisture",
            null, null, "Draining mix", null, null, null, true);
        Assert.Throws<ArgumentException>(() => plant.SetCatalogProfile(profile));
        plant.Update("Example", "Example species", "Example plant",
            plant.SunlightRequirement, plant.SunlightInstructions, 7, plant.WateringInstructions,
            null, null, plant.SoilInstructions, null, null, null, false);
        plant.SetCatalogProfile(profile);
        plant.Update("Example", "Example species", "Example plant",
            plant.SunlightRequirement, plant.SunlightInstructions, 7, plant.WateringInstructions,
            null, null, plant.SoilInstructions, null, null, null, true);
        Assert.Null(plant.CatalogProfile!.NonToxicToCatsAndDogsSourceUrl);
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
