using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PlantCare.Api.IntegrationTests.Infrastructure;
using PlantCare.Infrastructure.Identity;
using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;
using PlantCare.Domain.ValueObjects;
using System.Net.Http.Json;
using PlantCare.Application.PlantCatalog;

namespace PlantCare.Api.IntegrationTests;

public sealed class BootstrapCommandTests : IAsyncLifetime
{
    private readonly PlantCareApiFactory factory = new();
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public Task DisposeAsync() => factory.DisposeAsync().AsTask();

    [Fact]
    public async Task AdminBootstrapRejectsMissingAndUnconfirmedAccounts()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            factory.RunOperatorCommandAsync("--bootstrap-admin", "missing@example.test"));
        await SeedAccountAsync(confirmed: false);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            factory.RunOperatorCommandAsync("--bootstrap-admin", "operator@example.test"));
        await factory.SeedAsync(async database => Assert.Empty(await database.UserRoles.ToListAsync()));
    }

    [Fact]
    public async Task ConfirmedAccountCanBePromotedRepeatedlyWithoutDuplicateRoles()
    {
        await SeedAccountAsync(confirmed: true);
        await factory.RunOperatorCommandAsync("--bootstrap-admin", "operator@example.test");
        await factory.RunOperatorCommandAsync("--bootstrap-admin", "operator@example.test");
        await factory.SeedAsync(async database =>
        {
            Assert.Single(await database.UserRoles.ToListAsync());
            Assert.Equal("Admin", (await database.Roles.SingleAsync()).Name);
        });
    }

    [Fact]
    public async Task CatalogImportIsRepeatableAndDoesNotOverwriteExistingEntries()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, Catalog);
            await factory.RunOperatorCommandAsync("--import-catalog", path);
            await File.WriteAllTextAsync(path, Catalog.Replace("Demo plant", "Changed name"));
            await factory.RunOperatorCommandAsync("--import-catalog", path);
            await factory.SeedAsync(async database =>
                Assert.Equal("Demo plant", (await database.PlantSpecies.SingleAsync()).CommonName));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task InvalidCatalogRollsBackAllEntries()
    {
        var path = Path.GetTempFileName();
        try
        {
            var invalid = Catalog.TrimEnd().TrimEnd(']') + "," +
                Catalog.TrimStart().TrimStart('[').Replace("Demo species", "Other species")
                    .Replace("\"defaultWateringIntervalDays\":7", "\"defaultWateringIntervalDays\":0");
            await File.WriteAllTextAsync(path, invalid);
            await Assert.ThrowsAsync<ValidationException>(() =>
                factory.RunOperatorCommandAsync("--import-catalog", path));
            await factory.SeedAsync(async database => Assert.Empty(await database.PlantSpecies.ToListAsync()));
        }
        finally { File.Delete(path); }
    }

    private Task SeedAccountAsync(bool confirmed) => factory.SeedAsync(database =>
    {
        database.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(), UserName = "operator@example.test",
            NormalizedUserName = "OPERATOR@EXAMPLE.TEST", Email = "operator@example.test",
            NormalizedEmail = "OPERATOR@EXAMPLE.TEST", EmailConfirmed = confirmed,
            SecurityStamp = Guid.NewGuid().ToString("N")
        });
        return Task.CompletedTask;
    });

    [Fact]
    public async Task CatalogUpsertRetainsPlantIdsAndPersonalSchedulesAndPersistsPhotoMetadata()
    {
        var path = Path.GetTempFileName();
        var plantId = Guid.Empty;
        var speciesId = Guid.Empty;
        try
        {
            await File.WriteAllTextAsync(path, Catalog);
            await factory.RunOperatorCommandAsync("--import-catalog", path);
            await SeedAccountAsync(confirmed: true);
            await factory.SeedAsync(async database =>
            {
                var species = await database.PlantSpecies.SingleAsync(); speciesId = species.Id;
                var user = await database.Users.SingleAsync();
                var plant = UserPlant.Create(user.Id, species.Id, "Keep my nickname", "Desk", null, "Keep my notes", PlantCareApiFactory.UtcNow);
                plant.AddCareSchedule(CareActionType.Watering, 11, PlantCareApiFactory.UtcNow);
                plantId = plant.Id; database.UserPlants.Add(plant);
            });
            var profile = new PlantCatalogProfile("https://plants.ces.ncsu.edu/plants/monstera-deliciosa/", new DateOnly(2026,10,21),
                new PlantCatalogPhoto("/images/plants/monstera-deliciosa.jpg", "A potted plant", "Test photographer", "https://plants.ces.ncsu.edu/plants/monstera-deliciosa/", "CC BY 2.0", "https://creativecommons.org/licenses/by/2.0/"));
            var node = System.Text.Json.Nodes.JsonNode.Parse(Catalog)!;
            node[0]!["commonName"] = "Reviewed plant";
            node[0]!["defaultWateringIntervalDays"] = 7;
            node[0]!["catalogProfile"] = System.Text.Json.JsonSerializer.SerializeToNode(profile, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
            await File.WriteAllTextAsync(path,node.ToJsonString());
            await factory.RunOperatorCommandAsync("--upsert-catalog", path);
            await factory.RunOperatorCommandAsync("--upsert-catalog", path);
            await factory.SeedAsync(async database =>
            {
                var species = await database.PlantSpecies.SingleAsync();
                Assert.Equal(speciesId,species.Id); Assert.Equal(profile,species.CatalogProfile);
                var plant = await database.UserPlants.Include(p=>p.CareSchedules).SingleAsync();
                Assert.Equal(plantId,plant.Id); Assert.Equal("Keep my nickname",plant.Nickname);
                Assert.Equal("Keep my notes",plant.Notes); Assert.Equal(11,Assert.Single(plant.CareSchedules).IntervalDays);
            });
            using var client=factory.CreateClient();
            var response=await client.GetFromJsonAsync<PlantSpeciesDto>($"/api/plant-species/{speciesId}",
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
                { Converters={new System.Text.Json.Serialization.JsonStringEnumConverter()} });
            Assert.Equal(profile,response!.CatalogProfile);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task InvalidPhotoLicenseOrRemoteImageRejectsWholeUpsert()
    {
        var path=Path.GetTempFileName();
        try
        {
            var node=System.Text.Json.Nodes.JsonNode.Parse(Catalog)!;
            node[0]!["catalogProfile"]=System.Text.Json.Nodes.JsonNode.Parse("""
                {"careSourceUrl":"https://example.org/care","reviewedOn":"2026-10-21","photo":{"path":"https://example.org/tracker.jpg","altText":"Plant","credit":"Test","sourceUrl":"https://example.org/source","licenseName":"CC BY-NC 4.0","licenseUrl":"https://creativecommons.org/licenses/by-nc/4.0/"}}
                """);
            await File.WriteAllTextAsync(path,node.ToJsonString());
            await Assert.ThrowsAsync<ArgumentException>(()=>factory.RunOperatorCommandAsync("--upsert-catalog",path));
            await factory.SeedAsync(async database=>Assert.Empty(await database.PlantSpecies.ToListAsync()));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ReviewedHouseplantDatasetImportsRepeatedlyWithCompleteCredits()
    {
        var path=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../deploy/catalog.houseplants.json"));
        Assert.True(File.Exists(path));
        await factory.RunOperatorCommandAsync("--upsert-catalog",path);
        await factory.RunOperatorCommandAsync("--upsert-catalog",path);
        await factory.SeedAsync(async database=>
        {
            var species=await database.PlantSpecies.ToListAsync();
            Assert.Equal(68,species.Count);
            Assert.Equal(8, species.Count(p => p.CatalogProfile?.GrowthReferences?.Count > 0));
            Assert.Equal(16, species.Sum(p => p.CatalogProfile?.GrowthReferences?.Count ?? 0));
            Assert.Equal(56, species.Count(p => p.CatalogProfile?.CommonNameAliases?.Count > 0));
            Assert.Equal(112, species.Sum(p => p.CatalogProfile?.CommonNameAliases?.Count ?? 0));
            Assert.Equal(13, species.Count(p => p.CatalogProfile?.NonToxicToCatsAndDogsSourceUrl is not null));
            Assert.DoesNotContain(species, p => p.IsToxicToPets && p.CatalogProfile?.NonToxicToCatsAndDogsSourceUrl is not null);
            foreach(var plant in species)
            {
                var profile=Assert.IsType<PlantCatalogProfile>(plant.CatalogProfile);
                profile.Validate();
                Assert.NotNull(profile.Photo);
                var image=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../plant-care-web/public",profile.Photo.Path.TrimStart('/')));
                Assert.True(File.Exists(image));
                foreach (var reference in profile.GrowthReferences ?? [])
                {
                    Assert.True(File.Exists(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                        "../../../../plant-care-web/public", reference.Photo.Path.TrimStart('/')))));
                }
            }
        });
        using var client = factory.CreateClient();
        var catalog = await client.GetFromJsonAsync<PlantSpeciesDto[]>("/api/plant-species",
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)
            { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } });
        var monstera = Assert.Single(catalog!, p => p.ScientificName == "Monstera deliciosa");
        var africanMask = Assert.Single(catalog!, p => p.CommonName == "African mask plant");
        Assert.Equal("Alocasia \u00d7 mortfontanensis", africanMask.ScientificName);
        Assert.Equal(2, monstera.CatalogProfile!.GrowthReferences!.Count);
        Assert.Contains("young", monstera.CatalogProfile.GrowthReferences[0].Photo.Path);
        var pothos = Assert.Single(catalog!, p => p.ScientificName == "Epipremnum aureum");
        Assert.Equal(["Devil's ivy", "Pothos"], pothos.CatalogProfile!.CommonNameAliases);
        var palm = Assert.Single(catalog!, p => p.ScientificName == "Chamaedorea elegans");
        Assert.Equal("https://www.aspca.org/pet-care/aspca-poison-control/toxic-and-non-toxic-plants/bamboo-palm",
            palm.CatalogProfile!.NonToxicToCatsAndDogsSourceUrl);
    }

    private const string Catalog = """
        [{"commonName":"Demo plant","scientificName":"Demo species",
          "description":"Synthetic test fixture.","sunlightRequirement":"BrightIndirectLight",
          "sunlightInstructions":"Demo only.","defaultWateringIntervalDays":7,
          "wateringInstructions":"Demo only.","soilInstructions":"Demo only.","isToxicToPets":false}]
        """;
}
