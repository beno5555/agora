using colosseumbackend.Common;
using colosseumbackend.Dtos;
using colosseumbackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace colosseumbackend.Controllers;

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
