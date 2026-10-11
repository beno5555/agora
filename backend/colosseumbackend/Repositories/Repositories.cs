using colosseumbackend.Common;
using colosseumbackend.Data;
using colosseumbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace colosseumbackend.Repositories;

public class UserRepository : BaseRepository<User>
{
    public UserRepository(AgoraDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<User?> GetByWalletAsync(string wallet)
    {
        return await _dbSet.FirstOrDefaultAsync(user => user.Wallet == wallet);
    }

    /// <summary>Creates the user on first sign-in; returns the stored row either way.</summary>
    public async Task<User> GetOrCreateAsync(string wallet)
    {
        var user = await GetByWalletAsync(wallet);
        if (user is null)
        {
            user = new User { Wallet = wallet };
            await AddAsync(user);
        }

        return user;
    }
}

public class AuthNonceRepository : BaseRepository<AuthNonce>
{
    public AuthNonceRepository(AgoraDbContext dbContext) : base(dbContext)
    {
    }

    public async Task ReplaceAsync(AuthNonce nonce)
    {
        await _dbSet.Where(existing => existing.Wallet == nonce.Wallet).ExecuteDeleteAsync();
        await AddAsync(nonce);
    }

    /// <summary>Deletes and returns the wallet's pending nonce, so each nonce works at most once.</summary>
    public async Task<AuthNonce?> TakeAsync(string wallet)
    {
        var nonce = await _dbSet.AsNoTracking().FirstOrDefaultAsync(existing => existing.Wallet == wallet);
        if (nonce is not null)
        {
            await _dbSet.Where(existing => existing.Wallet == wallet).ExecuteDeleteAsync();
        }

        return nonce;
    }
}

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

public class NotificationRepository : BaseRepository<Notification>
{
    public NotificationRepository(AgoraDbContext dbContext) : base(dbContext)
    {
    }

    public async Task AddRangeAsync(IEnumerable<Notification> notifications)
    {
        await _dbSet.AddRangeAsync(notifications);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<Notification>> GetForWalletAsync(string wallet, int pageNumber, int pageSize)
    {
        int size = ClampPageSize(pageSize);
        return await _dbSet.AsNoTracking()
            .Where(notification => notification.Wallet == wallet)
            .OrderByDescending(notification => notification.CreatedAt)
            .Skip((Math.Max(pageNumber, 1) - 1) * size)
            .Take(size)
            .ToListAsync();
    }

    /// <summary>Returns false when the notification doesn't exist or isn't the caller's.</summary>
    public async Task<bool> MarkReadAsync(long id, string wallet)
    {
        int updated = await _dbSet.Where(notification => notification.Id == id && notification.Wallet == wallet)
                                  .ExecuteUpdateAsync(setters => setters.SetProperty(notification => notification.ReadAt, DateTime.UtcNow));
        return updated > 0;
    }
}

public class ActionLogRepository : BaseRepository<ActionLog>
{
    public ActionLogRepository(AgoraDbContext dbContext) : base(dbContext)
    {
    }
}
