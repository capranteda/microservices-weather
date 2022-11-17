using Microsoft.EntityFrameworkCore;

namespace Cloudweather.Temperature.DataAccess;

public class TemperatureDbContext : DbContext
{
    public TemperatureDbContext()
    {
    }
    
    public TemperatureDbContext(DbContextOptions<TemperatureDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Temperature> Temperature { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        SnakeCaseIdentityTableNames(modelBuilder);
    }

    private static void SnakeCaseIdentityTableNames(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Temperature>(b=> b.ToTable("temperature"));
    }
}