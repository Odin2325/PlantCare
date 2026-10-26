using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Application.MyPlants;
namespace PlantCare.Api.Controllers;

[ApiController, Authorize, Route("api/my-plants/harvest-overview")]
public sealed class HarvestOverviewController(IHarvestService harvests) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<HarvestOverviewDto>> Get([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var user) || user == Guid.Empty) return Unauthorized();
        if (from == default || to == default || to < from || to.DayNumber - from.DayNumber > 365)
            return BadRequest("Choose a valid date range of at most 366 days.");
        return Ok(await harvests.GetOverviewAsync(user, from, to, ct));
    }
}
