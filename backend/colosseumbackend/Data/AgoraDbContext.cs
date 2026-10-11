using colosseumbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace colosseumbackend.Data;

public class AgoraDbContext : DbContext
{
    public AgoraDbContext(DbContextOptions<AgoraDbContext> options) : base(options)
    {
    }

    public DbSet<User>         Users         => Set<User>();
    public DbSet<AuthNonce>    AuthNonces    => Set<AuthNonce>();
    public DbSet<Game>         Games         => Set<Game>();
    public DbSet<Listing>      Listings      => Set<Listing>();
    public DbSet<Deal>         Deals         => Set<Deal>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ActionLog>    ActionLogs    => Set<ActionLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AgoraDbContext).Assembly);
    }
}
