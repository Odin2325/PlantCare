using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Application.MyPlants;
using System.Security.Claims;

namespace PlantCare.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/my-plants/{id:guid}/growing-context")]
public sealed class GrowingContextsController(IUserPlantService plants) : ControllerBase
{
    [HttpPut]
    [ProducesResponseType(typeof(UserPlantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserPlantDto>> Update(Guid id, UpdateGrowingContextCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        try
        {
            var result = await plants.UpdateGrowingContextAsync(id, userId, request, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException)
        {
            return BadRequest(new ProblemDetails { Title = "Choose a supported environment and care season, or leave them unset.", Status = 400 });
        }
    }
}
