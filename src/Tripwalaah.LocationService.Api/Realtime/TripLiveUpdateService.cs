using Microsoft.AspNetCore.SignalR;
using Tripwalaah.LocationService.Api.Hubs;
using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Application.Realtime;

namespace Tripwalaah.LocationService.Api.Realtime;

public sealed class TripLiveUpdateService(
    IHubContext<TripHub> hubContext,
    ITripPresenceStore presenceStore,
    ILiveLocationCache liveLocationCache,
    ITripEventPublisher eventPublisher) : ITripLiveUpdateService
{
    public async Task BroadcastLocationAsync(
        LiveLocationUpdateDto update,
        CancellationToken cancellationToken = default)
    {
        await liveLocationCache.SaveAsync(update, cancellationToken);
        await eventPublisher.PublishLocationUpdatedAsync(update, cancellationToken);
        await hubContext.Clients
            .Group(TripHubGroups.ForTrip(update.TripId))
            .SendAsync(TripHubClientEvents.LocationUpdated, update, cancellationToken);
    }

    public async Task BroadcastStatusAsync(
        TripStatusUpdateDto update,
        CancellationToken cancellationToken = default)
    {
        await eventPublisher.PublishTripStatusUpdatedAsync(update, cancellationToken);
        await hubContext.Clients
            .Group(TripHubGroups.ForTrip(update.TripId))
            .SendAsync(TripHubClientEvents.TripStatusUpdated, update, cancellationToken);
    }

    public async Task NotifyMemberJoinedAsync(
        TripMemberEventDto memberEvent,
        CancellationToken cancellationToken = default)
    {
        await eventPublisher.PublishMemberJoinedAsync(memberEvent, cancellationToken);
        await hubContext.Clients
            .Group(TripHubGroups.ForTrip(memberEvent.TripId))
            .SendAsync(TripHubClientEvents.MemberJoined, memberEvent, cancellationToken);
    }

    public async Task NotifyMemberLeftAsync(
        TripMemberEventDto memberEvent,
        CancellationToken cancellationToken = default)
    {
        await liveLocationCache.RemoveAsync(memberEvent.TripId, memberEvent.UserId, cancellationToken);
        await eventPublisher.PublishMemberLeftAsync(memberEvent, cancellationToken);
        await hubContext.Clients
            .Group(TripHubGroups.ForTrip(memberEvent.TripId))
            .SendAsync(TripHubClientEvents.MemberLeft, memberEvent, cancellationToken);
    }

    public async Task SendPresenceSnapshotAsync(
        string tripId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        var members = await presenceStore.GetMembersAsync(tripId, cancellationToken);
        var cachedLocations = await liveLocationCache.GetTripLocationsAsync(tripId, cancellationToken);
        var byUser = cachedLocations.ToDictionary(x => x.UserId, StringComparer.Ordinal);

        var enriched = members
            .Select(member =>
                byUser.TryGetValue(member.UserId, out var location)
                    ? member with { LastLocation = location }
                    : member)
            .ToList();

        var snapshot = new TripPresenceSnapshotDto(tripId, enriched, DateTime.UtcNow);

        await hubContext.Clients
            .Client(connectionId)
            .SendAsync(TripHubClientEvents.PresenceSnapshot, snapshot, cancellationToken);
    }
}
