using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Application.Notifications;

namespace PlantCare.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/push")]
public sealed class PushSubscriptionsController(IPushSubscriptionService service, IConfiguration configuration) : ControllerBase
{
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        var publicKey = configuration["WebPush:PublicKey"];
        return Ok(new { isEnabled = !string.IsNullOrWhiteSpace(publicKey), publicKey = publicKey ?? string.Empty });
    }

    [HttpPost("subscriptions")]
    public async Task<ActionResult<PushSubscriptionDto>> Save(SavePushSubscriptionCommand command, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await service.SaveAsync(userId, command, cancellationToken));
    }

    [HttpDelete("subscriptions/{subscriptionId:guid}")]
    public async Task<IActionResult> Remove(Guid subscriptionId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return await service.RemoveAsync(userId, subscriptionId, cancellationToken) ? NoContent() : NotFound();
    }

    private bool TryGetUserId(out Guid userId) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
