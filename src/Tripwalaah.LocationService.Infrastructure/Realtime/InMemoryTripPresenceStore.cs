using System.Collections.Concurrent;
using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;

namespace Tripwalaah.LocationService.Infrastructure.Realtime;

public sealed class InMemoryTripPresenceStore : ITripPresenceStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, TripMemberPresenceDto>> _trips = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, (string TripId, string UserId)> _connections = new(StringComparer.Ordinal);

    public Task AddOrUpdateAsync(
        string tripId,
        string userId,
        string connectionId,
        string? displayName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tripId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        tripId = tripId.Trim();
        userId = userId.Trim();

        var members = _trips.GetOrAdd(tripId, _ => new ConcurrentDictionary<string, TripMemberPresenceDto>(StringComparer.Ordinal));

        members.AddOrUpdate(
            userId,
            _ => new TripMemberPresenceDto(tripId, userId, displayName, connectionId, DateTime.UtcNow, null),
            (_, existing) => existing with
            {
                TripId = tripId,
                DisplayName = displayName ?? existing.DisplayName,
                ConnectionId = connectionId,
                JoinedAt = existing.JoinedAt
            });

        _connections[connectionId] = (tripId, userId);
        return Task.CompletedTask;
    }

    public Task<TripMemberPresenceDto?> RemoveByConnectionAsync(
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        if (!_connections.TryRemove(connectionId, out var mapping))
        {
            return Task.FromResult<TripMemberPresenceDto?>(null);
        }

        return RemoveInternalAsync(mapping.TripId, mapping.UserId, connectionId);
    }

    public Task RemoveAsync(
        string tripId,
        string userId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        _connections.TryRemove(connectionId, out _);
        return RemoveInternalAsync(tripId, userId, connectionId);
    }

    public Task<LiveLocationUpdateDto?> UpdateLocationAsync(
        string tripId,
        string userId,
        double latitude,
        double longitude,
        double? speedKmh,
        double? heading,
        CancellationToken cancellationToken = default)
    {
        if (!_trips.TryGetValue(tripId, out var members) || !members.TryGetValue(userId, out var member))
        {
            return Task.FromResult<LiveLocationUpdateDto?>(null);
        }

        var update = new LiveLocationUpdateDto(
            tripId,
            userId,
            member.DisplayName,
            latitude,
            longitude,
            speedKmh,
            heading,
            DateTime.UtcNow);

        members[userId] = member with { LastLocation = update };
        return Task.FromResult<LiveLocationUpdateDto?>(update);
    }

    public Task<IReadOnlyList<TripMemberPresenceDto>> GetMembersAsync(
        string tripId,
        CancellationToken cancellationToken = default)
    {
        if (!_trips.TryGetValue(tripId, out var members))
        {
            return Task.FromResult<IReadOnlyList<TripMemberPresenceDto>>([]);
        }

        IReadOnlyList<TripMemberPresenceDto> snapshot = members.Values
            .OrderBy(x => x.JoinedAt)
            .ToList();

        return Task.FromResult(snapshot);
    }

    public Task<string?> GetUserIdByConnectionAsync(
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        if (_connections.TryGetValue(connectionId, out var mapping))
        {
            return Task.FromResult<string?>(mapping.UserId);
        }

        return Task.FromResult<string?>(null);
    }

    private Task<TripMemberPresenceDto?> RemoveInternalAsync(string tripId, string userId, string connectionId)
    {
        if (!_trips.TryGetValue(tripId, out var members))
        {
            return Task.FromResult<TripMemberPresenceDto?>(null);
        }

        if (!members.TryGetValue(userId, out var member))
        {
            return Task.FromResult<TripMemberPresenceDto?>(null);
        }

        // Only remove if this connection still owns the seat (user may have reconnected).
        if (!string.Equals(member.ConnectionId, connectionId, StringComparison.Ordinal))
        {
            return Task.FromResult<TripMemberPresenceDto?>(null);
        }

        members.TryRemove(userId, out var removed);

        if (members.IsEmpty)
        {
            _trips.TryRemove(tripId, out _);
        }

        return Task.FromResult<TripMemberPresenceDto?>(removed);
    }
}
