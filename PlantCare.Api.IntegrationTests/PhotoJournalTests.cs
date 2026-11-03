using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlantCare.Api.IntegrationTests.Infrastructure;
using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;
using PlantCare.Infrastructure.Identity;
using PlantCare.Infrastructure.Persistence;
using PlantCare.Infrastructure.Photos;
using PlantCare.PhotoTransport;

namespace PlantCare.Api.IntegrationTests;

public sealed class PhotoJournalTests : IAsyncLifetime
{
    private const string Password = "SyntheticPhotoPassword9!";
    private readonly PlantCareApiFactory factory = new(useRealAuthentication: true,
        pushTestSqlPassword: Environment.GetEnvironmentVariable("PLANTCARE_PUSH_TEST_SQL_PASSWORD"),
        customizeServices: services => { services.RemoveAll<PhotoJournalLimits>(); services.AddSingleton(new PhotoJournalLimits(2, 3, 48, 64)); });
    private static DateOnly Date => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
    private static readonly byte[] Jpeg = [255,216,1,2,3,4,255,217];
    private static ProcessedPhoto Photo => new(Jpeg, Jpeg, 8, 4);
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public async Task DisposeAsync() { try { await factory.DeletePushTestDatabaseAsync(); } finally { await factory.DisposeAsync(); } }
    private async Task<ApplicationUser> User()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var name = Guid.NewGuid().ToString("N") + "@example.test";
        var user = new ApplicationUser { UserName = name, Email = name, EmailConfirmed = true };
        Assert.True((await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().CreateAsync(user, Password)).Succeeded);
        return user;
    }
    private async Task<UserPlant> Plant(Guid owner)
    {
        var species = PlantSpecies.Create("Synthetic photo species", null, "Test", SunlightRequirement.BrightIndirectLight,
            "Test", 7, "Test", null, null, "Test", null, null, null, false);
        var plant = UserPlant.Create(owner, species.Id, "Photo fixture", null, null, null, DateTimeOffset.UtcNow);
        await factory.SeedAsync(db => { db.PlantSpecies.Add(species); db.UserPlants.Add(plant); return Task.CompletedTask; });
        return plant;
    }
    private async Task<HttpClient> Client(ApplicationUser user)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        await client.AddAntiforgeryTokenAsync();
        (await client.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email = user.Email, password = Password })).EnsureSuccessStatusCode();
        await client.AddAntiforgeryTokenAsync(); return client;
    }
    private async Task<PhotoEntryDto?> Save(Guid owner, Guid plant, Guid? request = null, string? notes = "Private photo note", PhotoJournalLimits? limits = null)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var store = limits is null ? scope.ServiceProvider.GetRequiredService<PhotoJournalStore>()
            : new PhotoJournalStore(scope.ServiceProvider.GetRequiredService<PlantCareDbContext>(), TimeProvider.System, limits);
        return await store.SaveNormalizedAsync(owner, plant, request ?? Guid.NewGuid(), Date, notes, Photo);
    }

    [Fact]
    public async Task StorageIsIdempotentPrivateAndDoesNotCreateCareOrUploads()
    {
        var a = await User(); var b = await User(); var plant = await Plant(a.Id); var otherPlant = await Plant(b.Id);
        var request = Guid.NewGuid(); var first = (await Save(a.Id, plant.Id, request))!;
        await factory.SeedAsync(db => { db.CalendarShares.Add(CalendarShare.Create(a.Id, b.Id, DateTimeOffset.UtcNow)); return Task.CompletedTask; });
        Assert.Equal(first, await Save(a.Id, plant.Id, request));
        await Assert.ThrowsAsync<PhotoJournalConflictException>(() => Save(a.Id, plant.Id, request, "Different note"));
        Assert.Null(await Save(b.Id, plant.Id));
        using var owner = await Client(a); using var other = await Client(b);
        var url = $"/api/my-plants/{plant.Id}/photos";
        var item = Assert.Single((await owner.GetFromJsonAsync<PhotoEntryDto[]>(url))!);
        Assert.Equal(first.Id, item.Id);
        using var response = await owner.GetAsync($"{url}/{item.Id}/image");
        response.EnsureSuccessStatusCode();
        Assert.Equal(Jpeg, await response.Content.ReadAsByteArrayAsync());
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType!.MediaType);
        Assert.True(response.Headers.CacheControl!.NoStore, response.Headers.CacheControl.ToString());
        Assert.True(response.Headers.CacheControl.Private, response.Headers.CacheControl.ToString());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"{url}/{item.Id}/image")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/my-plants/{otherPlant.Id}/photos/{item.Id}/thumbnail")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PostAsync(url, new MultipartFormDataContent())).StatusCode);
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(url)).StatusCode);
        await factory.SeedAsync(async db => { Assert.Single(await db.PlantPhotoEntries.ToArrayAsync()); Assert.Single(await db.PlantPhotoMedia.ToArrayAsync());
            Assert.Empty(await db.CareSchedules.ToArrayAsync()); Assert.Empty(await db.Notifications.ToArrayAsync()); Assert.Empty(await db.HarvestRecords.ToArrayAsync()); });
    }

    [Fact]
    public async Task InvalidMetadataAndUnboundedMediaCannotCreatePartialRows()
    {
        var owner = await User(); var plant = await Plant(owner.Id);
        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<PhotoJournalStore>();
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveNormalizedAsync(owner.Id, plant.Id, Guid.Empty, Date, null, Photo));
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveNormalizedAsync(owner.Id, plant.Id, Guid.NewGuid(), Date, new string('a',501), Photo));
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveNormalizedAsync(owner.Id, plant.Id, Guid.NewGuid(), Date.AddDays(10), null, Photo));
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveNormalizedAsync(owner.Id, plant.Id, Guid.NewGuid(), Date, null,
            new ProcessedPhoto(new byte[524289], Jpeg, 8, 4)));
        var db = scope.ServiceProvider.GetRequiredService<PlantCareDbContext>();
        Assert.Empty(await db.PlantPhotoEntries.ToArrayAsync()); Assert.Empty(await db.PlantPhotoMedia.ToArrayAsync());
    }

    [Fact]
    public async Task CookieWritesVersionChecksArchivedReadAndDeletionPreserveBoundaries()
    {
        var a = await User(); var plant = await Plant(a.Id); var request = Guid.NewGuid(); var entry = (await Save(a.Id, plant.Id, request))!;
        using var client = await Client(a); var url = $"/api/my-plants/{plant.Id}/photos/{entry.Id}";
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync(url, new { entry.Version, photographedOn = Date, notes = "Updated" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/account/export/photos", new { password = Password })).StatusCode);
        await client.AddAntiforgeryTokenAsync();
        var updatedResponse = await client.PatchAsJsonAsync(url, new { entry.Version, photographedOn = Date, notes = "Updated" }); updatedResponse.EnsureSuccessStatusCode();
        var updated = (await updatedResponse.Content.ReadFromJsonAsync<PhotoEntryDto>())!;
        Assert.NotEqual(entry.Version, updated.Version);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PatchAsJsonAsync(url, new { entry.Version, photographedOn = Date, notes = "Stale" })).StatusCode);
        Assert.Equal("Updated", (await Save(a.Id, plant.Id, request))!.Notes); // Retry never overwrites a later edit.
        await factory.SeedAsync(async db => { (await db.UserPlants.SingleAsync()).Archive(); });
        (await client.GetAsync(url + "/thumbnail")).EnsureSuccessStatusCode();
        await Assert.ThrowsAsync<PhotoJournalConflictException>(() => Save(a.Id, plant.Id));
        (await client.DeleteAsync(url)).EnsureSuccessStatusCode(); (await client.DeleteAsync(url)).EnsureSuccessStatusCode();
        await factory.SeedAsync(async db => { Assert.Empty(await db.PlantPhotoEntries.ToArrayAsync()); Assert.Empty(await db.PlantPhotoMedia.ToArrayAsync()); });
    }

    [Fact]
    public async Task CountAndByteQuotasAreAtomicAndDeletionReclaimsSpace()
    {
        var a = await User(); var b = await User(); var c = await User();
        var p = await Plant(a.Id); var p2 = await Plant(a.Id); var q = await Plant(b.Id); var r = await Plant(c.Id);
        var first = (await Save(a.Id, p.Id))!; await Save(a.Id, p.Id);
        await Assert.ThrowsAsync<PhotoJournalConflictException>(() => Save(a.Id, p.Id));
        await Save(a.Id, p2.Id);
        await Assert.ThrowsAsync<PhotoJournalConflictException>(() => Save(a.Id, p2.Id));
        await Save(b.Id, q.Id);
        await Assert.ThrowsAsync<PhotoJournalConflictException>(() => Save(c.Id, r.Id));
        await using (var scope = factory.Services.CreateAsyncScope())
            Assert.True(await scope.ServiceProvider.GetRequiredService<PhotoJournalStore>().DeleteAsync(a.Id, p.Id, first.Id, default));
        await Save(c.Id, r.Id);
        // Lowering the account byte cap blocks a new write independently of count caps.
        await Assert.ThrowsAsync<PhotoJournalConflictException>(() => Save(c.Id, r.Id, limits: new(20, 50, 16, 512 * 1024 * 1024)));
        await factory.SeedAsync(async db => { Assert.Equal(4, await db.PlantPhotoEntries.CountAsync()); Assert.Equal(4, await db.PlantPhotoMedia.CountAsync()); });
    }

    [Fact]
    public async Task ConcurrentSqlWritesCannotSpendTheLastServiceSlotTwice()
    {
        if (Environment.GetEnvironmentVariable("PLANTCARE_PUSH_TEST_SQL_PASSWORD") is null) return;
        var a = await User(); var b = await User(); var c = await User();
        var p = await Plant(a.Id); var p2 = await Plant(a.Id); var q = await Plant(b.Id); var r = await Plant(c.Id);
        await Save(a.Id, p.Id); await Save(a.Id, p.Id); await Save(a.Id, p2.Id);
        async Task<bool> Attempt(Guid owner, Guid id)
        {
            try { return await Save(owner, id) is not null; } catch (PhotoJournalConflictException) { return false; }
        }
        var result = await Task.WhenAll(Attempt(b.Id, q.Id), Attempt(c.Id, r.Id));
        Assert.Single(result, success => success);
        await factory.SeedAsync(async db => Assert.Equal(64, await db.PlantPhotoMedia.SumAsync(p => (long)p.Image.Length + p.Thumbnail.Length)));
    }

    [Fact]
    public async Task JsonZipExportAndAccountErasureIncludeOnlyTheOwner()
    {
        var a = await User(); var b = await User(); var p = await Plant(a.Id); var q = await Plant(b.Id);
        var photoA = (await Save(a.Id, p.Id))!; var photoB = (await Save(b.Id, q.Id))!;
        using var client = await Client(a);
        using var json = JsonDocument.Parse(await (await client.PostAsJsonAsync("/api/account/export", new { password = Password })).Content.ReadAsStringAsync());
        Assert.Equal(photoA.Id, json.RootElement.GetProperty("photoJournal").EnumerateArray().Single().GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/account/export/photos", new { password = "WrongPassword9!" })).StatusCode);
        for (var repeat = 0; repeat < 2; repeat++)
        {
            using var response = await client.PostAsJsonAsync("/api/account/export/photos", new { password = Password }); response.EnsureSuccessStatusCode();
            Assert.True(response.Headers.CacheControl!.NoStore);
            using var zip = new ZipArchive(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
            Assert.Equal(2, zip.Entries.Count);
            var file = zip.GetEntry($"photos/{photoA.Id:N}.jpg")!;
            using var bytes = new MemoryStream(); await file.Open().CopyToAsync(bytes); Assert.Equal(Jpeg, bytes.ToArray());
            using var manifest = JsonDocument.Parse(zip.GetEntry("manifest.json")!.Open());
            Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(Jpeg)), manifest.RootElement.GetProperty("images")[0].GetProperty("sha256").GetString());
            Assert.Null(zip.GetEntry($"photos/{photoB.Id:N}.jpg"));
        }
        (await client.PostAsJsonAsync("/api/account/delete", new { password = Password, confirmation = "DELETE" })).EnsureSuccessStatusCode();
        await factory.SeedAsync(async db => { Assert.Equal(photoB.Id, Assert.Single(await db.PlantPhotoEntries.IgnoreQueryFilters().ToArrayAsync()).Id);
            Assert.Equal(photoB.Id, Assert.Single(await db.PlantPhotoMedia.IgnoreQueryFilters().ToArrayAsync()).Id); });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/my-plants/{p.Id}/photos/{photoA.Id}/image")).StatusCode);
    }

    [Fact]
    public async Task SqlSnapshotFingerprintsActualBytesAndWorkerHasNoJournalPermissions()
    {
        if (Environment.GetEnvironmentVariable("PLANTCARE_PUSH_TEST_SQL_PASSWORD") is null) return;
        var owner = await User(); var plant = await Plant(owner.Id); var entry = (await Save(owner.Id, plant.Id))!;
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlantCareDbContext>();
        var connection = db.Database.GetDbConnection();
        Assert.StartsWith("PlantCarePushTests_", connection.Database);
        await connection.OpenAsync();
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "PlantCare.slnx"))) root = root.Parent;
        var script = File.ReadAllText(Path.Combine(root!.FullName, "scripts", "LocalBackup.Common.ps1"));
        var match = Regex.Match(script, @"(?s)\$BackupSnapshotSql = @'\r?\n(.*?)\r?\n'@");
        Assert.True(match.Success);
        async Task<string> Hash()
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "DROP TABLE IF EXISTS #state;\n" + match.Groups[1].Value.Replace("USE [PlantCare];", "");
            await using var reader = await command.ExecuteReaderAsync();
            do
            {
                if (reader.FieldCount == 3 && reader.GetFieldType(1) == typeof(string))
                    while (await reader.ReadAsync())
                        if (string.Equals(reader.GetString(0), entry.Id.ToString(), StringComparison.OrdinalIgnoreCase)) return reader.GetString(1);
            } while (await reader.NextResultAsync());
            throw new InvalidOperationException("Photo fingerprint missing.");
        }
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Jpeg)), await Hash());
        var changed = Jpeg.ToArray(); changed[2] ^= 1;
        await db.PlantPhotoMedia.ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Image, changed));
        Assert.Equal(Convert.ToHexString(SHA256.HashData(changed)), await Hash());

        // Database-local users only; never create/alter the server's real runtime logins.
        foreach (var role in new[] { "Api", "Worker" })
        {
            var policy = (Dictionary<string, string[]>)typeof(PlantCare.Api.Operations.DatabaseCommands)
                .GetMethod(role + "Policy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.Invoke(null, null)!;
            var name = "photo_test_" + Guid.NewGuid().ToString("N");
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE USER [{name}] WITHOUT LOGIN;" + string.Join("", policy.Select(item =>
                $"GRANT {string.Join(',', item.Value)} ON OBJECT::[dbo].[{item.Key}] TO [{name}];"));
            await command.ExecuteNonQueryAsync();
            command.CommandText = $"EXECUTE AS USER = '{name}';"; await command.ExecuteNonQueryAsync();
            try
            {
                foreach (var table in new[] { "PlantPhotoEntries", "PlantPhotoMedia", "PhotoJournalQuota" })
                    foreach (var permission in new[] { "SELECT", "INSERT", "UPDATE", "DELETE" })
                    {
                        command.CommandText = $"SELECT HAS_PERMS_BY_NAME('dbo.{table}', 'OBJECT', '{permission}');";
                        var permitted = Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
                        Assert.Equal(role == "Api" && policy[table].Contains(permission), permitted);
                    }
            }
            finally { command.CommandText = "REVERT;"; await command.ExecuteNonQueryAsync(); }
        }
    }

    [Fact]
    public async Task TombstonesHideMetadataAndBytesAndCorruptMediaIsNeverReturned()
    {
        var a = await User(); var p = await Plant(a.Id); var item = (await Save(a.Id, p.Id))!;
        using var client = await Client(a);
        var corrupt = Jpeg.ToArray(); corrupt[2] ^= 1;
        await factory.SeedAsync(async db => { await db.PlantPhotoMedia.ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Image, corrupt)); });
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync($"/api/my-plants/{p.Id}/photos/{item.Id}/image")).StatusCode);
        await factory.SeedAsync(db => { db.AccountErasures.Add(AccountErasure.Create(a.Id, DateTimeOffset.UtcNow)); return Task.CompletedTask; });
        await factory.SeedAsync(async db => { Assert.Empty(await db.PlantPhotoEntries.ToArrayAsync()); Assert.Empty(await db.PlantPhotoMedia.ToArrayAsync());
            Assert.Single(await db.PlantPhotoEntries.IgnoreQueryFilters().ToArrayAsync()); Assert.Single(await db.PlantPhotoMedia.IgnoreQueryFilters().ToArrayAsync()); });
        var status = (await client.GetAsync($"/api/my-plants/{p.Id}/photos/{item.Id}/image")).StatusCode;
        Assert.True(status is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized);
        Assert.Null(await Save(a.Id, p.Id));
    }
}
