using colosseumbackend.Common;
using colosseumbackend.Data;
using colosseumbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace colosseumbackend.Repositories;

public class ListingRepository : BaseRepository<Listing>
{
    private const string LikeEscape = "\\";

    public ListingRepository(AgoraDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<List<Listing>> SearchActiveAsync(int? gameId, ListingType? type, string? textQuery, int pageNumber, int pageSize)
    {
        var query = _dbSet.AsNoTracking()
            .Include(listing => listing.Seller)
            .Include(listing => listing.Game)
            .Where(listing => listing.IsActive);

        if (gameId is not null)
        {
            query = query.Where(listing => listing.GameId == gameId);
        }

        if (type is not null)
        {
            query = query.Where(listing => listing.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(textQuery))
        {
            // SQLite's LIKE ignores case (ASCII); Contains would translate to the case-sensitive instr().
            string pattern = "%" + EscapeLikePattern(textQuery) + "%";
            query = query.Where(listing => EF.Functions.Like(listing.Title, pattern, LikeEscape)
                                        || EF.Functions.Like(listing.Description, pattern, LikeEscape));
        }

        int size = ClampPageSize(pageSize);
        return await query.OrderByDescending(listing => listing.CreatedAt)
                          .Skip((Math.Max(pageNumber, 1) - 1) * size)
                          .Take(size)
                          .ToListAsync();
    }

    public async Task<Listing?> GetByIdAsync(Guid id)
    {
        return await _dbSet.Include(listing => listing.Seller)
                           .Include(listing => listing.Game)
                           .FirstOrDefaultAsync(listing => listing.Id == id);
    }

    /// <summary>Only the seller's own listing; others' listings look the same as missing ones.</summary>
    public async Task<Listing?> GetOwnedAsync(Guid id, string sellerWallet)
    {
        return await _dbSet.Include(listing => listing.Seller)
                           .Include(listing => listing.Game)
                           .FirstOrDefaultAsync(listing => listing.Id == id && listing.SellerWallet == sellerWallet);
    }

    /// <summary>All of the seller's listings, paused ones included, newest first.</summary>
    public async Task<List<Listing>> GetBySellerAsync(string sellerWallet, int pageNumber, int pageSize)
    {
        int size = ClampPageSize(pageSize);
        return await _dbSet.AsNoTracking()
            .Include(listing => listing.Game)
            .Include(listing => listing.Seller)
            .Where(listing => listing.SellerWallet == sellerWallet)
            .OrderByDescending(listing => listing.CreatedAt)
            .Skip((Math.Max(pageNumber, 1) - 1) * size)
            .Take(size)
            .ToListAsync();
    }

    public async Task<List<Listing>> GetActiveBySellerAsync(string sellerWallet)
    {
        return await _dbSet.AsNoTracking()
            .Include(listing => listing.Game)
            .Include(listing => listing.Seller)
            .Where(listing => listing.SellerWallet == sellerWallet && listing.IsActive)
            .OrderByDescending(listing => listing.CreatedAt)
            .ToListAsync();
    }

    /// <summary>Makes the user's text match literally, so % and _ aren't treated as wildcards.</summary>
    private static string EscapeLikePattern(string text)
    {
        return text.Replace(LikeEscape, LikeEscape + LikeEscape)
                   .Replace("%", LikeEscape + "%")
                   .Replace("_", LikeEscape + "_");
    }
}
