using colosseumbackend.Common;
using colosseumbackend.Dtos;
using colosseumbackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace colosseumbackend.Controllers;

[Route("api/me/notifications")]
[Authorize]
public class NotificationController : BaseController
{
    private readonly NotificationService _notificationService;

    public NotificationController(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    [EnableRateLimiting(RateLimitConfig.Policies.Read)]
    public async Task<ActionResult<ListResponse<NotificationDto>>> GetMine([FromQuery] PageQuery pageQuery)
    {
        return Ok(await _notificationService.GetMineAsync(GetWallet(), pageQuery));
    }

    [HttpPost("{id:long}/read")]
    [EnableRateLimiting(RateLimitConfig.Policies.Write)]
    public async Task<ActionResult<ApplicationResponse>> MarkRead(long id)
    {
        var response = await _notificationService.MarkReadAsync(id, GetWallet());

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }
}
