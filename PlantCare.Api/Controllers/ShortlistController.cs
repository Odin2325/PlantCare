using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantCare.Domain.Entities;
using PlantCare.Infrastructure.Persistence;

namespace PlantCare.Api.Controllers;

[ApiController, Authorize, Route("api/shortlist")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ShortlistController(PlantCareDbContext db, TimeProvider time) : ControllerBase
{
    public sealed record Item(Guid PlantSpeciesId, DateTimeOffset SavedAtUtc);

    private async Task<Guid?> Owner(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return null;
        return await db.Users.AnyAsync(u => u.Id == id, ct) ? id : null;
    }

    [HttpGet]
    public async Task<ActionResult<List<Item>>> List(CancellationToken ct)
    {
        if (await Owner(ct) is not { } owner) return Unauthorized();
        return await db.ShortlistedPlants.AsNoTracking().Where(p => p.UserId == owner)
            .OrderByDescending(p => p.SavedAtUtc).ThenBy(p => p.PlantSpeciesId)
            .Select(p => new Item(p.PlantSpeciesId, p.SavedAtUtc)).ToListAsync(ct);
    }

    [HttpPut("{plantSpeciesId:guid}")]
    public async Task<IActionResult> Save(Guid plantSpeciesId, CancellationToken ct)
    {
        if (await Owner(ct) is not { } owner) return Unauthorized();
        if (!await db.PlantSpecies.AnyAsync(p => p.Id == plantSpeciesId, ct)) return NotFound();
        if (await db.ShortlistedPlants.AnyAsync(p => p.UserId == owner && p.PlantSpeciesId == plantSpeciesId, ct)) return NoContent();
        db.ShortlistedPlants.Add(ShortlistedPlant.Create(owner, plantSpeciesId, time.GetUtcNow()));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            // Concurrent saves of the same public plant are idempotent. Other
            // database failures (including a concurrently erased owner) still fail.
            db.ChangeTracker.Clear();
            if (!await db.ShortlistedPlants.AnyAsync(p => p.UserId == owner && p.PlantSpeciesId == plantSpeciesId, ct)) throw;
        }
        return NoContent();
    }

    [HttpDelete("{plantSpeciesId:guid}")]
    public async Task<IActionResult> Remove(Guid plantSpeciesId, CancellationToken ct)
    {
        if (await Owner(ct) is not { } owner) return Unauthorized();
        await db.ShortlistedPlants.Where(p => p.UserId == owner && p.PlantSpeciesId == plantSpeciesId).ExecuteDeleteAsync(ct);
        return NoContent();
    }
}
