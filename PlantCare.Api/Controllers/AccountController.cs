using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Contracts.Authentication;
using PlantCare.Infrastructure.Identity;

namespace PlantCare.Api.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public sealed class AccountController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> GetMe()
    {
        var user = await userManager.GetUserAsync(User);

        if (user?.Email is null)
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserResponse(Id: user.Id, Email: user.Email));
    }
}