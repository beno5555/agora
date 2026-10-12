using colosseumbackend.Common;
using colosseumbackend.Dtos;
using colosseumbackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace colosseumbackend.Controllers;

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
