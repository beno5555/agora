using colosseumbackend.Common;
using colosseumbackend.Data;
using colosseumbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace colosseumbackend.Repositories;

public class ListingRepository : BaseRepository<Listing>
{
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
            query = query.Where(listing => listing.Title.Contains(textQuery) || listing.Description.Contains(textQuery));
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
}
