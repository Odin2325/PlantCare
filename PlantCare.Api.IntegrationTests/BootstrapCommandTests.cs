using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using PlantCare.Api.IntegrationTests.Infrastructure;
using PlantCare.Infrastructure.Identity;

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

    private const string Catalog = """
        [{"commonName":"Demo plant","scientificName":"Demo species",
          "description":"Synthetic test fixture.","sunlightRequirement":"BrightIndirectLight",
          "sunlightInstructions":"Demo only.","defaultWateringIntervalDays":7,
          "wateringInstructions":"Demo only.","soilInstructions":"Demo only.","isToxicToPets":false}]
        """;
}
