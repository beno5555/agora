using colosseumbackend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

    /// <summary>
    /// Every stored time is UTC, but SQLite reads it back as DateTimeKind.Unspecified, which JSON
    /// writes without a "Z" and browsers then parse as local time. Mark it UTC on the way out.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    private class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter() : base(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
        {
        }
    }
}
