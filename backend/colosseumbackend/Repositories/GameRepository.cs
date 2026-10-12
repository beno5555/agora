using colosseumbackend.Data;
using colosseumbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace colosseumbackend.Repositories;

public class GameRepository : BaseRepository<Game>
{
    public GameRepository(AgoraDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<List<(Game Game, int ActiveListings)>> GetAllWithListingCountsAsync()
    {
        var rows = await _dbSet.AsNoTracking()
            .OrderBy(game => game.Name)
            .Select(game => new
            {
                Game           = game,
                ActiveListings = _dbContext.Listings.Count(listing => listing.GameId == game.Id && listing.IsActive),
            })
            .ToListAsync();

        return rows.Select(row => (row.Game, row.ActiveListings)).ToList();
    }

    public async Task<Game?> GetBySlugAsync(string slug)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(game => game.Slug == slug);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _dbSet.AnyAsync(game => game.Id == id);
    }
}
