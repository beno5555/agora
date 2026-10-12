using colosseumbackend.Common;
using colosseumbackend.Data;
using colosseumbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace colosseumbackend.Repositories;

public class DealRepository : BaseRepository<Deal>
{
    private static readonly DealStatus[] OpenStatuses =
        [DealStatus.Draft, DealStatus.AwaitingSeller, DealStatus.Active, DealStatus.Delivered, DealStatus.Disputed];

    public DealRepository(AgoraDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>Only a deal the caller is party to; others' deals look the same as missing ones.</summary>
    public async Task<Deal?> GetForPartyAsync(Guid id, string wallet)
    {
        return await _dbSet.Include(deal => deal.Listing)
                           .ThenInclude(listing => listing!.Game)
                           .FirstOrDefaultAsync(deal => deal.Id == id && (deal.BuyerWallet == wallet || deal.SellerWallet == wallet));
    }

    public async Task<List<Deal>> GetForUserAsync(string wallet, bool? asBuyer, DealStatus? status, int pageNumber, int pageSize)
    {
        var query = _dbSet.AsNoTracking()
            .Include(deal => deal.Listing)
            .ThenInclude(listing => listing!.Game)
            .Where(deal => deal.Status != DealStatus.Draft && deal.Status != DealStatus.Expired);

        if (asBuyer is null)
        {
            query = query.Where(deal => deal.BuyerWallet == wallet || deal.SellerWallet == wallet);
        }
        else if (asBuyer.Value)
        {
            query = query.Where(deal => deal.BuyerWallet == wallet);
        }
        else
        {
            query = query.Where(deal => deal.SellerWallet == wallet);
        }

        if (status is not null)
        {
            query = query.Where(deal => deal.Status == status);
        }

        int size = ClampPageSize(pageSize);
        return await query.OrderByDescending(deal => deal.CreatedAt)
                          .Skip((Math.Max(pageNumber, 1) - 1) * size)
                          .Take(size)
                          .ToListAsync();
    }

    /// <summary>Open deals for the background sync, oldest-synced first. Internal use only.</summary>
    public async Task<List<Deal>> GetOpenAsync(int limit)
    {
        return await _dbSet.Where(deal => OpenStatuses.Contains(deal.Status))
                           .OrderBy(deal => deal.SyncedAt)
                           .Take(limit)
                           .ToListAsync();
    }
}
