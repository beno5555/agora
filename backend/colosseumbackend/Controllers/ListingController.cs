using colosseumbackend.Common;
using colosseumbackend.Dtos;
using colosseumbackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace colosseumbackend.Controllers;

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

    [HttpGet("~/api/me/listings")]
    [EnableRateLimiting(RateLimitConfig.Policies.Read)]
    public async Task<ActionResult<ListResponse<FullListingDto>>> GetMine([FromQuery] PageQuery pageQuery)
    {
        return Ok(await _listingService.GetMineAsync(GetWallet(), pageQuery));
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
