using colosseumbackend.Data;
using colosseumbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace colosseumbackend.Repositories;

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
