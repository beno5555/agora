using colosseumbackend.Data;
using Microsoft.EntityFrameworkCore;

namespace colosseumbackend.Repositories;

public abstract class BaseRepository<T> where T : class
{
    protected readonly AgoraDbContext _dbContext;
    protected readonly DbSet<T>       _dbSet;

    protected BaseRepository(AgoraDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSet     = dbContext.Set<T>();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    protected static int ClampPageSize(int pageSize)
    {
        return Math.Clamp(pageSize, 1, Common.Constants.MaxPageSize);
    }
}
