using colosseumbackend.Data;
using colosseumbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace colosseumbackend.Repositories;

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
