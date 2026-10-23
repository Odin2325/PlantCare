using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PlantCare.Infrastructure.Persistence;

namespace PlantCare.Api.Operations;

public static class AccountErasureCommands
{
    public sealed record Marker(Guid UserId, DateTimeOffset ErasedAtUtc);
    public sealed record Ledger(int Version, string Source, DateTimeOffset CapturedAtUtc, Marker[] Markers, Guid[] ExistingUserIds);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    public static bool IsCommand(string[] args) => args.Length > 0 &&
        args[0] is "--export-account-erasures" or "--apply-account-erasures";

    public static async Task ExecuteAsync(IServiceProvider services, IConfiguration config, string[] args)
    {
        var db = services.GetRequiredService<PlantCareDbContext>();
        if (args is ["--export-account-erasures", var source, var output])
        {
            ValidateSource(source);
            var exportedLedger = await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                var records = await db.AccountErasures.AsNoTracking().OrderBy(e => e.UserId)
                    .Select(e => new Marker(e.UserId, e.ErasedAtUtc)).ToArrayAsync();
                var users = await db.Users.AsNoTracking().OrderBy(u => u.Id).Select(u => u.Id).ToArrayAsync();
                var result = new Ledger(1, source, DateTimeOffset.UtcNow, records, users);
                await tx.CommitAsync(); return result;
            });
            await using var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(output, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            await JsonSerializer.SerializeAsync(file, exportedLedger, Json);
            file.Flush(flushToDisk: true);
            return;
        }
        if (args is not ["--apply-account-erasures", var input] ||
            !config.GetValue<bool>("Recovery:AllowAccountErasureApply"))
            throw new InvalidOperationException("Erasure reconciliation requires an explicit isolated-recovery configuration.");
        var expected = config["Recovery:AccountErasureSource"] ?? "";
        ValidateSource(expected);
        Ledger ledger;
        try
        {
            if (new FileInfo(input).Length is < 1 or > 16_777_216) throw new InvalidOperationException("Erasure ledger size is invalid.");
            await using var stream = File.OpenRead(input);
            ledger = await JsonSerializer.DeserializeAsync<Ledger>(stream, Json) ?? throw new InvalidOperationException("Erasure ledger is empty.");
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        { throw new InvalidOperationException("Current erasure ledger is missing or invalid; recovery must remain isolated."); }
        var now = DateTimeOffset.UtcNow;
        var liveIds = ledger.ExistingUserIds?.ToHashSet();
        if (ledger.Version != 1 || ledger.Source != expected || ledger.CapturedAtUtc < now.AddMinutes(-15) ||
            ledger.CapturedAtUtc > now.AddMinutes(1) || ledger.Markers is null || ledger.Markers.Length > 100_000 ||
            ledger.Markers.Any(m => m is null || m.UserId == Guid.Empty || m.ErasedAtUtc > now.AddMinutes(1) || m.ErasedAtUtc.Year < 2000) ||
            ledger.Markers.Select(m => m.UserId).Distinct().Count() != ledger.Markers.Length ||
            ledger.ExistingUserIds is null || ledger.ExistingUserIds.Length > 100_000 ||
            ledger.ExistingUserIds.Any(id => id == Guid.Empty) || liveIds!.Count != ledger.ExistingUserIds.Length ||
            ledger.Markers.Any(m => liveIds.Contains(m.UserId)))
            throw new InvalidOperationException("Erasure ledger is stale, mismatched or invalid; refresh it from the authoritative source.");

        var markerIds = ledger.Markers.Select(m => m.UserId).ToHashSet();
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var existing = (await db.AccountErasures.Select(e => e.UserId).ToArrayAsync()).ToHashSet();
            foreach (var marker in ledger.Markers)
                if (!existing.Contains(marker.UserId))
                    db.AccountErasures.Add(PlantCare.Domain.Entities.AccountErasure.Create(marker.UserId, marker.ErasedAtUtc));
            // Complete live membership also protects deletions predating the
            // journal: never revive a restored identity absent from the source.
            var live = liveIds!;
            var restored = await db.Users.IgnoreQueryFilters().Select(u => u.Id).ToArrayAsync();
            foreach (var id in restored.Where(id => !live.Contains(id) && !existing.Contains(id) && !markerIds.Contains(id)))
                db.AccountErasures.Add(PlantCare.Domain.Entities.AccountErasure.Create(id, ledger.CapturedAtUtc));
            await db.SaveChangesAsync();
            // Include markers already present in the target, even if the source
            // ledger predates a target marker. No tombstone is removed or replaced.
            var ids = await db.AccountErasures.Select(e => e.UserId).ToArrayAsync();
            foreach (var batch in ids.Chunk(500))
            {
                await db.CalendarShares.IgnoreQueryFilters().Where(s => batch.Contains(s.OwnerUserId) || batch.Contains(s.RecipientUserId)).ExecuteDeleteAsync();
                var emails = await db.Users.IgnoreQueryFilters().Where(u => batch.Contains(u.Id)).Select(u => u.NormalizedEmail).ToArrayAsync();
                await db.CalendarInvitations.IgnoreQueryFilters().Where(i => batch.Contains(i.OwnerUserId) || emails.Contains(i.NormalizedRecipientEmail)).ExecuteDeleteAsync();
                await db.Users.IgnoreQueryFilters().Where(u => batch.Contains(u.Id)).ExecuteDeleteAsync();
            }
            if (await db.Users.IgnoreQueryFilters().AnyAsync(u => db.AccountErasures.Any(e => e.UserId == u.Id)))
                throw new InvalidOperationException("Erased identities remain; recovered services must stay stopped.");
            await tx.CommitAsync();
        });
    }

    private static void ValidateSource(string source)
    {
        if (source is not ("plantcare-local" or "plantcare-vps-staging"))
            throw new InvalidOperationException("Unknown erasure ledger source.");
    }
}
