using Tripwalaah.LocationService.Domain.Entities;

namespace Tripwalaah.LocationService.Application.Interfaces;

public interface ILocationRepository
{
    Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Location> Items, int TotalCount)> SearchAsync(
        string? query,
        string? countryCode,
        LocationType? type,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(Location location, CancellationToken cancellationToken = default);

    Task UpdateAsync(Location location, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
