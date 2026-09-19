using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Contracts.MyPlants;
using PlantCare.Application.Care;
using PlantCare.Application.MyPlants;
using PlantCare.Domain.Enums;
using System.Security.Claims;

namespace PlantCare.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/my-plants")]
public sealed class MyPlantsController(IUserPlantService userPlantService, ICareService careService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserPlantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<UserPlantDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var userPlants = await userPlantService.GetAllAsync(userId, cancellationToken);

        return Ok(userPlants);
    }

    [HttpGet("archived")]
    [ProducesResponseType(
        typeof(IReadOnlyList<UserPlantDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserPlantDto>>>
        GetArchived(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var userPlants = await userPlantService
            .GetArchivedAsync(userId, cancellationToken);

        return Ok(userPlants);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserPlantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserPlantDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var userPlant = await userPlantService.GetByIdAsync(id, userId, cancellationToken);

        if (userPlant is null)
        {
            return NotFound();
        }

        return Ok(userPlant);
    }

    [HttpPost("{id:guid}/care/{actionType}/complete")]
    [ProducesResponseType(
    typeof(CompleteCareActionResult),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompleteCareActionResult>>
    CompleteCareAction(Guid id, CareActionType actionType, CompleteCareActionRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        if (actionType == CareActionType.Unknown || !Enum.IsDefined(typeof(CareActionType), actionType))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid care action type."
            });
        }

        CompleteCareActionResult? result;

        try
        {
            result = await careService.CompleteAsync(
                userId,
                id,
                actionType,
                request.CompletedAtUtc,
                request.Notes,
                cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid care completion.",
                Detail = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Care action cannot be completed.",
                Detail = exception.Message
            });
        }

        if (result is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Care schedule not found.",
                Detail =
                    "The plant does not have the requested care schedule."
            });
        }

        return Ok(result);
    }

    [HttpPut("{id:guid}/care/{actionType}/schedule")]
    [ProducesResponseType(
        typeof(CareScheduleDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CareScheduleDto>> UpdateCareSchedule(
        Guid id,
        CareActionType actionType,
        UpdateCareScheduleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await careService.UpdateScheduleAsync(
            userId,
            id,
            actionType,
            request.IntervalDays,
            request.IsEnabled,
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/care/{actionType}/schedule")]
    [ProducesResponseType(
        typeof(CareScheduleDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CareScheduleDto>> AddCareSchedule(
        Guid id,
        CareActionType actionType,
        CreateCareScheduleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await careService.AddScheduleAsync(
            userId,
            id,
            actionType,
            request.IntervalDays,
            cancellationToken);

        return result is null
            ? NotFound()
            : StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("{id:guid}/care/{actionType}/schedule")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveCareSchedule(
        Guid id,
        CareActionType actionType,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var archived = await careService.ArchiveScheduleAsync(
            userId,
            id,
            actionType,
            cancellationToken);

        return archived ? NoContent() : NotFound();
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
            Notes: request.Notes,
            WateringIntervalDays:
                request.WateringIntervalDays,
            LastWateredAtUtc:
                request.LastWateredAtUtc,
            FertilizingIntervalDays:
                request.FertilizingIntervalDays,
            LastFertilizedAtUtc:
                request.LastFertilizedAtUtc);

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

    [HttpGet("{id:guid}/care/history")]
    [ProducesResponseType(
    typeof(IReadOnlyList<CareEventHistoryDto>),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status404NotFound)]
    public async Task<
    ActionResult<IReadOnlyList<CareEventHistoryDto>>>
    GetCareHistory(
        Guid id,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        if (take is < 1 or > 100)
        {
            ModelState.AddModelError(
                nameof(take),
                "Take must be between 1 and 100.");

            return ValidationProblem(ModelState);
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

        var history =
            await careService.GetHistoryAsync(
                userId,
                id,
                take,
                cancellationToken);

        return Ok(history);
    }

    [HttpPut("{id:guid}/care/history/{eventId:guid}")]
    [ProducesResponseType(
        typeof(CareEventHistoryDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CareEventHistoryDto>> UpdateCareEvent(
        Guid id,
        Guid eventId,
        UpdateCareEventRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await careService.UpdateEventAsync(
            userId,
            id,
            eventId,
            request.CompletedAtUtc!.Value,
            request.Notes,
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}/care/history/{eventId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCareEvent(
        Guid id,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var deleted = await careService.DeleteEventAsync(
            userId,
            id,
            eventId,
            cancellationToken);

        return deleted ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var archived = await userPlantService.ArchiveAsync(id, userId, cancellationToken);

        return archived ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var restored = await userPlantService.RestoreAsync(
            id,
            userId,
            cancellationToken);

        return restored ? NoContent() : NotFound();
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out userId);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserPlantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserPlantDto>> Update(Guid id, UpdateUserPlantRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var command = new UpdateUserPlantCommand(
            Nickname: request.Nickname,
            Location: request.Location,
            AcquiredOn: request.AcquiredOn,
            Notes: request.Notes);

        var updated = await userPlantService.UpdateAsync(id, userId, command, cancellationToken);

        return updated is null ? NotFound() : Ok(updated);
    }
}
