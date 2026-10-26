using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Application.MyPlants;
namespace PlantCare.Api.Controllers;

[ApiController, Authorize, Route("api/my-plants/{plantId:guid}/harvests")]
public sealed class HarvestsController(IHarvestService harvests) : ControllerBase
{
    private Guid Owner => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HarvestRecordDto>>> Get(Guid plantId, CancellationToken ct)
    {
        if (Owner == Guid.Empty)
            return Unauthorized();
        var result = await harvests.GetAsync(Owner, plantId, ct);
        return result is null ? NotFound() : Ok(result);
    }
    [HttpPost]
    public async Task<ActionResult<HarvestRecordDto>> Add(Guid plantId, RecordHarvestCommand command, CancellationToken ct)
    {
        if (Owner == Guid.Empty)
            return Unauthorized();
        var result = await harvests.AddAsync(Owner, plantId, command, ct);
        return result is null ? NotFound() : Ok(result);
    }
    [HttpDelete("{recordId:guid}")]
    public async Task<IActionResult> Delete(Guid plantId, Guid recordId, CancellationToken ct)
    {
        if (Owner == Guid.Empty)
            return Unauthorized();
        return await harvests.DeleteAsync(Owner, plantId, recordId, ct) ? NoContent() : NotFound();
    }
}