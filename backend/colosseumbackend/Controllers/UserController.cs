using colosseumbackend.Common;
using colosseumbackend.Dtos;
using colosseumbackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace colosseumbackend.Controllers;

[Route("api")]
[Authorize]
public class UserController : BaseController
{
    private readonly UserService _userService;

    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    [EnableRateLimiting(RateLimitConfig.Policies.Read)]
    public async Task<ActionResult<ApplicationResponse<FullUserDto>>> GetMe()
    {
        var response = await _userService.GetMeAsync(GetWallet());

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    [HttpPatch("me")]
    [EnableRateLimiting(RateLimitConfig.Policies.Write)]
    public async Task<ActionResult<ApplicationResponse<FullUserDto>>> UpdateMe([FromBody] EditUserDto editUserDto)
    {
        var response = await _userService.UpdateMeAsync(GetWallet(), editUserDto);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    [HttpGet("users/{wallet}")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitConfig.Policies.Read)]
    public async Task<ActionResult<ApplicationResponse<StandardUserDto>>> GetProfile(string wallet)
    {
        var response = await _userService.GetProfileAsync(wallet);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }
}
