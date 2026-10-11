using System.Security.Claims;
using colosseumbackend.Common;
using colosseumbackend.Configuration;
using colosseumbackend.Dtos;
using colosseumbackend.Services;
using colosseumbackend.Solana;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace colosseumbackend.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    /// <summary>The caller's wallet; only call on authorized endpoints, where the claim always exists.</summary>
    protected string GetWallet() => User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

    protected string? GetWalletIfSignedIn() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}

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

[Route("api/config")]
[EnableRateLimiting(RateLimitConfig.Policies.Read)]
public class ConfigController : BaseController
{
    private readonly SolanaGateway _solana;
    private readonly SolanaOptions _solanaOptions;

    public ConfigController(SolanaGateway solana, IOptions<SolanaOptions> solanaOptions)
    {
        _solana        = solana;
        _solanaOptions = solanaOptions.Value;
    }

    [HttpGet]
    public async Task<ActionResult<ApplicationResponse<ConfigDto>>> Get()
    {
        var config   = await _solana.GetConfigAsync();
        var response = new ApplicationResponse<ConfigDto>();
        response.Ok(new ConfigDto
        {
            Cluster      = _solanaOptions.Cluster,
            RpcUrl       = _solanaOptions.RpcUrl,
            ProgramId    = _solanaOptions.ProgramId,
            Mint         = _solanaOptions.Mint,
            MintDecimals = _solanaOptions.MintDecimals,
            FeeBps       = config?.FeeBps,
        }, ResponseMessages.ConfigRetrieved);

        return Ok(response);
    }
}

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

[Route("api/games")]
[EnableRateLimiting(RateLimitConfig.Policies.Read)]
public class GameController : BaseController
{
    private readonly GameService _gameService;

    public GameController(GameService gameService)
    {
        _gameService = gameService;
    }

    [HttpGet]
    public async Task<ActionResult<ListResponse<GameDto>>> GetAll()
    {
        return Ok(await _gameService.GetAllAsync());
    }
}

[Route("api/listings")]
[Authorize]
public class ListingController : BaseController
{
    private readonly ListingService _listingService;

    public ListingController(ListingService listingService)
    {
        _listingService = listingService;
    }

    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitConfig.Policies.Read)]
    public async Task<ActionResult<ListResponse<MinimalListingDto>>> Search([FromQuery] ListingSearchQuery query)
    {
        return Ok(await _listingService.SearchAsync(query));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitConfig.Policies.Read)]
    public async Task<ActionResult<ApplicationResponse<StandardListingDto>>> GetById(Guid id)
    {
        var response = await _listingService.GetByIdAsync(id, GetWalletIfSignedIn());

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitConfig.Policies.Write)]
    public async Task<ActionResult<ApplicationResponse<StandardListingDto>>> Create([FromBody] CreateListingDto createListingDto)
    {
        var response = await _listingService.CreateAsync(GetWallet(), createListingDto);

        if (response.Succeeded)
        {
            return CreatedAtAction(nameof(GetById), new { response.Data!.Id }, response);
        }

        return BadRequest(response);
    }

    [HttpPatch("{id:guid}")]
    [EnableRateLimiting(RateLimitConfig.Policies.Write)]
    public async Task<ActionResult<ApplicationResponse<StandardListingDto>>> Update(Guid id, [FromBody] EditListingDto editListingDto)
    {
        var response = await _listingService.UpdateAsync(id, GetWallet(), editListingDto);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response); // not found and invalid terms both land here
    }

    [HttpPost("{id:guid}/pause")]
    [EnableRateLimiting(RateLimitConfig.Policies.Write)]
    public async Task<ActionResult<ApplicationResponse>> Pause(Guid id)
    {
        var response = await _listingService.SetActiveAsync(id, GetWallet(), false);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    [HttpPost("{id:guid}/resume")]
    [EnableRateLimiting(RateLimitConfig.Policies.Write)]
    public async Task<ActionResult<ApplicationResponse>> Resume(Guid id)
    {
        var response = await _listingService.SetActiveAsync(id, GetWallet(), true);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }
}

[Route("api")]
[Authorize]
public class DealController : BaseController
{
    private readonly DealService _dealService;

    public DealController(DealService dealService)
    {
        _dealService = dealService;
    }

    [HttpPost("deals")]
    [EnableRateLimiting(RateLimitConfig.Policies.Write)]
    public async Task<ActionResult<ApplicationResponse<DealTransactionDto>>> Create([FromBody] CreateDealDto createDealDto)
    {
        var response = await _dealService.CreateAsync(GetWallet(), createDealDto);

        if (response.Succeeded)
        {
            return CreatedAtAction(nameof(GetById), new { response.Data!.Deal.Id }, response);
        }

        return BadRequest(response);
    }

    [HttpGet("deals/{id:guid}")]
    [EnableRateLimiting(RateLimitConfig.Policies.Read)]
    public async Task<ActionResult<ApplicationResponse<FullDealDto>>> GetById(Guid id)
    {
        var response = await _dealService.GetAsync(id, GetWallet());

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    [HttpGet("me/deals")]
    [EnableRateLimiting(RateLimitConfig.Policies.Read)]
    public async Task<ActionResult<ListResponse<MinimalDealDto>>> GetMine([FromQuery] DealQuery dealQuery)
    {
        return Ok(await _dealService.GetMineAsync(GetWallet(), dealQuery));
    }

    [HttpPost("deals/{id:guid}/tx")]
    [EnableRateLimiting(RateLimitConfig.Policies.Write)]
    public async Task<ActionResult<ApplicationResponse<DealTransactionDto>>> BuildTransaction(Guid id, [FromBody] DealActionDto dealActionDto)
    {
        var response = await _dealService.BuildActionTransactionAsync(id, GetWallet(), dealActionDto);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response); // not found, not allowed and invalid input all land here
    }

    [HttpPost("deals/{id:guid}/sync")]
    [EnableRateLimiting(RateLimitConfig.Policies.Write)]
    public async Task<ActionResult<ApplicationResponse<FullDealDto>>> Sync(Guid id, [FromBody] SyncDealDto syncDealDto)
    {
        var response = await _dealService.SyncAsync(id, GetWallet(), syncDealDto);

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
}

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
