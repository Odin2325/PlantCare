using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Contracts.MyPlants;
using PlantCare.Application.MyPlants;

namespace PlantCare.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/my-plants")]
public sealed class MyPlantsController(
    IUserPlantService userPlantService)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<UserPlantDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<
        ActionResult<IReadOnlyList<UserPlantDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var userPlants =
            await userPlantService.GetAllAsync(
                userId,
                cancellationToken);

        return Ok(userPlants);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(UserPlantDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserPlantDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var userPlant =
            await userPlantService.GetByIdAsync(
                id,
                userId,
                cancellationToken);

        if (userPlant is null)
        {
            return NotFound();
        }

        return Ok(userPlant);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(UserPlantDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserPlantDto>> Add(
        AddUserPlantRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        if (request.PlantSpeciesId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(request.PlantSpeciesId),
                "A valid plant species ID is required.");

            return ValidationProblem(ModelState);
        }

        var command = new AddUserPlantCommand(
            PlantSpeciesId: request.PlantSpeciesId,
            Nickname: request.Nickname,
            Location: request.Location,
            AcquiredOn: request.AcquiredOn,
            Notes: request.Notes);

        var createdUserPlant =
            await userPlantService.AddAsync(
                userId,
                command,
                cancellationToken);

        if (createdUserPlant is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Plant species not found.",
                Detail =
                    "The requested plant species does not exist."
            });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdUserPlant.Id },
            createdUserPlant);
    }

    private bool TryGetCurrentUserId(
        out Guid userId)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            userIdValue,
            out userId);
    }
}