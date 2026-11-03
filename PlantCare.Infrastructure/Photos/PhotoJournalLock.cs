using Microsoft.EntityFrameworkCore;
using PlantCare.Infrastructure.Persistence;

namespace PlantCare.Infrastructure.Photos;

public static class PhotoJournalLock
{
    // Call first inside every journal/cascading-erasure transaction, before
    // reading account/plant rows. A missing migration must fail closed.
    public static async Task AcquireAsync(PlantCareDbContext db, CancellationToken ct = default)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Photo locking requires a transaction.");
        if (await db.Database.ExecuteSqlRawAsync("UPDATE [PhotoJournalQuota] SET [Revision] = [Revision] + 1 WHERE [Id] = 1", ct) != 1)
            throw new InvalidOperationException("Photo journal quota authority is unavailable.");
    }
}
