using Microsoft.AspNetCore.Mvc;

namespace PlantCare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SystemController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            Status = "Healthy",
            Application = "PlantCare.Api",
            UtcTime = DateTimeOffset.UtcNow
        });
    }
}