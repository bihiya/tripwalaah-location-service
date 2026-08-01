using Microsoft.AspNetCore.SignalR;
using Tripwalaah.LocationService.Api.Hubs;
using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Application.Realtime;

namespace Tripwalaah.LocationService.Api.Realtime;

public sealed class TripLiveUpdateService(
    IHubContext<TripHub> hubContext,
    ITripPresenceStore presenceStore) : ITripLiveUpdateService
{
    public Task BroadcastLocationAsync(
        LiveLocationUpdateDto update,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients
            .Group(TripHubGroups.ForTrip(update.TripId))
            .SendAsync(TripHubClientEvents.LocationUpdated, update, cancellationToken);

    public Task BroadcastStatusAsync(
        TripStatusUpdateDto update,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients
            .Group(TripHubGroups.ForTrip(update.TripId))
            .SendAsync(TripHubClientEvents.TripStatusUpdated, update, cancellationToken);

    public Task NotifyMemberJoinedAsync(
        TripMemberEventDto memberEvent,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients
            .Group(TripHubGroups.ForTrip(memberEvent.TripId))
            .SendAsync(TripHubClientEvents.MemberJoined, memberEvent, cancellationToken);

    public Task NotifyMemberLeftAsync(
        TripMemberEventDto memberEvent,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients
            .Group(TripHubGroups.ForTrip(memberEvent.TripId))
            .SendAsync(TripHubClientEvents.MemberLeft, memberEvent, cancellationToken);

    public async Task SendPresenceSnapshotAsync(
        string tripId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        var members = await presenceStore.GetMembersAsync(tripId, cancellationToken);
        var snapshot = new TripPresenceSnapshotDto(tripId, members, DateTime.UtcNow);

        await hubContext.Clients
            .Client(connectionId)
            .SendAsync(TripHubClientEvents.PresenceSnapshot, snapshot, cancellationToken);
    }
}
