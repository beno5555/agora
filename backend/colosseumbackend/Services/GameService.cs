using colosseumbackend.Common;
using colosseumbackend.Dtos;
using colosseumbackend.Mappers;
using colosseumbackend.Repositories;

namespace colosseumbackend.Services;

public class GameService
{
    private readonly GameRepository _gameRepository;

    public GameService(GameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<ListResponse<GameDto>> GetAllAsync()
    {
        var response = new ListResponse<GameDto>();
        var games    = await _gameRepository.GetAllWithListingCountsAsync();

        response.Ok(games.Select(row => GameMapper.ToDto(row.Game, row.ActiveListings)).ToList(), ResponseMessages.GamesRetrieved);
        return response;
    }
}
