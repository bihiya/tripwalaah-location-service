using Microsoft.EntityFrameworkCore;
using Tripwalaah.LocationService.Domain.Entities;

namespace Tripwalaah.LocationService.Infrastructure.Persistence;

public sealed class LocationDbContext(DbContextOptions<LocationDbContext> options) : DbContext(options)
{
    public DbSet<Location> Locations => Set<Location>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("locations");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.City).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Country).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CountryCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Region).HasMaxLength(120);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.Timezone).HasMaxLength(80);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);

            entity.HasIndex(x => x.CountryCode);
            entity.HasIndex(x => x.City);
            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => new { x.Latitude, x.Longitude });
        });
    }
}
