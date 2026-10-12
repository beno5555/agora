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
