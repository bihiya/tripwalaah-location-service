using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;

namespace Tripwalaah.LocationService.Infrastructure.Redis;

/// <summary>Fallback when Redis is disabled (e.g. unit tests).</summary>
public sealed class NullLiveLocationCache : ILiveLocationCache
{
    public Task SaveAsync(LiveLocationUpdateDto update, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<LiveLocationUpdateDto?> GetAsync(
        string tripId,
        string userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<LiveLocationUpdateDto?>(null);

    public Task<IReadOnlyList<LiveLocationUpdateDto>> GetTripLocationsAsync(
        string tripId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LiveLocationUpdateDto>>([]);

    public Task RemoveAsync(
        string tripId,
        string userId,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
