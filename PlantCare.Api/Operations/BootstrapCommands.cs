using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Contracts.PlantTypes;
using PlantCare.Application.PlantCatalog;
using PlantCare.Infrastructure.Identity;
using PlantCare.Infrastructure.Persistence;

namespace PlantCare.Api.Operations;

public static class BootstrapCommands
{
    public static bool IsOperatorCommand(string[] args) =>
        args.Length > 0 && args[0] is "--bootstrap-admin" or "--import-catalog" or "--upsert-catalog";

    public static async Task ExecuteAsync(IServiceProvider services, string[] args)
    {
        if (!IsOperatorCommand(args) || args.Length != 2 || string.IsNullOrWhiteSpace(args[1]))
            throw new ArgumentException("Use --bootstrap-admin EMAIL, --import-catalog JSON_PATH or --upsert-catalog JSON_PATH.");

        if (args[0] == "--bootstrap-admin")
        {
            var users = services.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(args[1]);
            if (user is null || !await users.IsEmailConfirmedAsync(user))
                throw new InvalidOperationException("Register and confirm the account before assigning Admin.");

            await RoleSeeder.SeedAsync(services);
            if (!await users.IsInRoleAsync(user, "Admin"))
            {
                var result = await users.AddToRoleAsync(user, "Admin");
                if (!result.Succeeded)
                    throw new InvalidOperationException("Admin assignment failed: " +
                        string.Join(", ", result.Errors.Select(error => error.Code)));
            }
            return;
        }

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            Converters = { new JsonStringEnumConverter() }
        };
        await using var input = File.OpenRead(args[1]);
        var entries = await JsonSerializer.DeserializeAsync<List<CreatePlantSpeciesRequest>>(input, options)
            ?? throw new ArgumentException("Catalog must be a JSON array.");
        if (entries.Count == 0)
            throw new ArgumentException("Catalog must contain at least one species.");

        // Validate the entire input before any writes. The transaction also rolls
        // back all additions if domain validation or database writes fail later.
        foreach (var entry in entries)
        {
            if (entry is null) throw new ArgumentException("Catalog cannot contain null entries.");
            Validator.ValidateObject(entry, new ValidationContext(entry), validateAllProperties: true);
            entry.CatalogProfile?.Validate();
        }

        var database = services.GetRequiredService<PlantCareDbContext>();
        var catalog = services.GetRequiredService<IPlantSpeciesService>();
        await using var transaction = await database.Database.BeginTransactionAsync();
        var current = (await catalog.GetAllAsync()).ToDictionary(species => Key(species.ScientificName, species.CommonName), StringComparer.OrdinalIgnoreCase);
        var existing = current.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (args[0] == "--upsert-catalog" && entries.Select(e => Key(e.ScientificName, e.CommonName)).Distinct(StringComparer.OrdinalIgnoreCase).Count() != entries.Count)
            throw new ArgumentException("An update catalog must not contain duplicate species keys.");
        foreach (var entry in entries)
        {
            var key = Key(entry.ScientificName, entry.CommonName);
            if (!existing.Add(key))
            {
                if (args[0] == "--upsert-catalog")
                    await catalog.UpdateAsync(current[key].Id, new UpdatePlantSpeciesCommand
                    {
                        CommonName=entry.CommonName, ScientificName=entry.ScientificName, Description=entry.Description,
                        SunlightRequirement=entry.SunlightRequirement, SunlightInstructions=entry.SunlightInstructions,
                        DefaultWateringIntervalDays=entry.DefaultWateringIntervalDays, WateringInstructions=entry.WateringInstructions,
                        DefaultFertilizingIntervalDays=entry.DefaultFertilizingIntervalDays, FertilizingInstructions=entry.FertilizingInstructions,
                        SoilInstructions=entry.SoilInstructions, HumidityInstructions=entry.HumidityInstructions,
                        MinimumTemperatureCelsius=entry.MinimumTemperatureCelsius, MaximumTemperatureCelsius=entry.MaximumTemperatureCelsius,
                        IsToxicToPets=entry.IsToxicToPets, CatalogProfile=entry.CatalogProfile
                    });
                continue;
            }
            await catalog.CreateAsync(new CreatePlantSpeciesCommand(
                entry.CommonName, entry.ScientificName, entry.Description, entry.SunlightRequirement,
                entry.SunlightInstructions, entry.DefaultWateringIntervalDays, entry.WateringInstructions,
                entry.DefaultFertilizingIntervalDays, entry.FertilizingInstructions, entry.SoilInstructions,
                entry.HumidityInstructions, entry.MinimumTemperatureCelsius,
                entry.MaximumTemperatureCelsius, entry.IsToxicToPets, entry.CatalogProfile));
        }
        await transaction.CommitAsync();
    }

    private static string Key(string? scientificName, string commonName) =>
        string.IsNullOrWhiteSpace(scientificName)
            ? "common:" + commonName.Trim()
            : "scientific:" + scientificName.Trim();
}
