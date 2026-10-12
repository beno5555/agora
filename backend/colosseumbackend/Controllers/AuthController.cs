using colosseumbackend.Common;
using colosseumbackend.Dtos;
using colosseumbackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace colosseumbackend.Controllers;

[Route("api/auth")]
[EnableRateLimiting(RateLimitConfig.Policies.Auth)]
public class AuthController : BaseController
{
    private readonly AuthService     _authService;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(AuthService authService, JwtTokenService jwtTokenService)
    {
        _authService     = authService;
        _jwtTokenService = jwtTokenService;
    }

    [HttpGet("nonce")]
    public async Task<ActionResult<ApplicationResponse<NonceResponseDto>>> GetNonce([FromQuery] string wallet)
    {
        var response = await _authService.IssueSignInMessageAsync(wallet);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpPost("verify")]
    public async Task<ActionResult<ApplicationResponse<FullUserDto>>> Verify([FromBody] VerifySignInDto verifySignInDto)
    {
        var response = await _authService.VerifySignInAsync(verifySignInDto);

        if (response.Succeeded)
        {
            var (token, expiresAt) = _jwtTokenService.CreateSessionToken(response.Data!.Wallet);
            Response.Cookies.Append(Constants.SessionCookieName, token, new CookieOptions
            {
                HttpOnly = true,
                Secure   = true,
                SameSite = SameSiteMode.Lax,
                Expires  = expiresAt,
            });
            return Ok(response);
        }

        return Unauthorized(response);
    }

    [HttpPost("logout")]
    public ActionResult Logout()
    {
        Response.Cookies.Delete(Constants.SessionCookieName);
        return NoContent();
    }
}
