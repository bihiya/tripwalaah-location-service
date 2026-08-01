using Tripwalaah.LocationService.Application.DTOs;

namespace Tripwalaah.LocationService.Application.Interfaces;

public interface ILiveLocationCache
{
    Task SaveAsync(LiveLocationUpdateDto update, CancellationToken cancellationToken = default);

    Task<LiveLocationUpdateDto?> GetAsync(
        string tripId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LiveLocationUpdateDto>> GetTripLocationsAsync(
        string tripId,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        string tripId,
        string userId,
        CancellationToken cancellationToken = default);
}
