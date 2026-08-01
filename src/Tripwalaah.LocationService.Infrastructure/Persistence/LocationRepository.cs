using Microsoft.EntityFrameworkCore;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Domain.Entities;

namespace Tripwalaah.LocationService.Infrastructure.Persistence;

public sealed class LocationRepository(LocationDbContext dbContext) : ILocationRepository
{
    public Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Locations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Location> Items, int TotalCount)> SearchAsync(
        string? query,
        string? countryCode,
        LocationType? type,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var locations = dbContext.Locations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLowerInvariant();
            locations = locations.Where(x =>
                x.Name.ToLower().Contains(term) ||
                x.City.ToLower().Contains(term) ||
                x.Country.ToLower().Contains(term) ||
                (x.Region != null && x.Region.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            var code = countryCode.Trim().ToUpperInvariant();
            locations = locations.Where(x => x.CountryCode == code);
        }

        if (type is not null)
        {
            locations = locations.Where(x => x.Type == type);
        }

        if (isActive is not null)
        {
            locations = locations.Where(x => x.IsActive == isActive);
        }

        var totalCount = await locations.CountAsync(cancellationToken);

        var items = await locations
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Location location, CancellationToken cancellationToken = default) =>
        await dbContext.Locations.AddAsync(location, cancellationToken);

    public Task UpdateAsync(Location location, CancellationToken cancellationToken = default)
    {
        dbContext.Locations.Update(location);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
