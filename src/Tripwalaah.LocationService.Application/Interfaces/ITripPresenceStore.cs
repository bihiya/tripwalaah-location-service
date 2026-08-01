using Tripwalaah.LocationService.Application.DTOs;

namespace Tripwalaah.LocationService.Application.Interfaces;

public interface ITripPresenceStore
{
    Task AddOrUpdateAsync(
        string tripId,
        string userId,
        string connectionId,
        string? displayName,
        CancellationToken cancellationToken = default);

    Task<TripMemberPresenceDto?> RemoveByConnectionAsync(
        string connectionId,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        string tripId,
        string userId,
        string connectionId,
        CancellationToken cancellationToken = default);

    Task<LiveLocationUpdateDto?> UpdateLocationAsync(
        string tripId,
        string userId,
        double latitude,
        double longitude,
        double? speedKmh,
        double? heading,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TripMemberPresenceDto>> GetMembersAsync(
        string tripId,
        CancellationToken cancellationToken = default);

    Task<string?> GetUserIdByConnectionAsync(
        string connectionId,
        CancellationToken cancellationToken = default);
}
