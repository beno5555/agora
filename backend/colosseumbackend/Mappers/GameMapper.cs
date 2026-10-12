using colosseumbackend.Data.Entities;
using colosseumbackend.Dtos;

namespace colosseumbackend.Mappers;

public static class GameMapper
{
    public static GameDto ToDto(Game game, int activeListings)
    {
        return new GameDto
        {
            Id             = game.Id,
            Slug           = game.Slug,
            Name           = game.Name,
            IconUrl        = game.IconUrl,
            ActiveListings = activeListings,
        };
    }
}
