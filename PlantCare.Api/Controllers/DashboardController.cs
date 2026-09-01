using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Application.Dashboard;
using System.Security.Claims;

namespace PlantCare.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(
    IDashboardService dashboardService)
    : ControllerBase
{
    [HttpGet("care")]
    [ProducesResponseType(typeof(IReadOnlyList<CareDueDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CareDueDto>>> GetCareDue([FromQuery] int daysAhead = 7, CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        if (daysAhead is < 0 or > 365)
        {
            return BadRequest(new ProblemDetails
            {
                Status =
                        StatusCodes.Status400BadRequest,
                Title =
                        "Invalid daysAhead.",
                Detail =
                        "daysAhead must be between 0 and 365."
            });
        }

        var result = await dashboardService.GetCareDueAsync(
                userId,
                daysAhead,
                cancellationToken);

        return Ok(result);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out userId);
    }
}