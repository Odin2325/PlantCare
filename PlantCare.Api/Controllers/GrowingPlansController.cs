using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Application.MyPlants;
using System.Security.Claims;

namespace PlantCare.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/my-plants/{id:guid}/growing-plan")]
public sealed class GrowingPlansController(IUserPlantService plants) : ControllerBase
{
    [HttpPut]
    [ProducesResponseType(typeof(UserPlantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserPlantDto>> Update(Guid id, UpdateGrowingPlanCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        try
        {
            var result = await plants.UpdateGrowingPlanAsync(id, userId, request, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException)
        {
            return BadRequest(new ProblemDetails { Title = "Plan dates must follow sowing, planting out and harvest order when provided.", Status = 400 });
        }
    }
}
