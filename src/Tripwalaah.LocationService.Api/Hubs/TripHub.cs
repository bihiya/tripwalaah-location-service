using Microsoft.AspNetCore.SignalR;
using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Application.Realtime;

namespace Tripwalaah.LocationService.Api.Hubs;

/// <summary>
/// Real-time hub for Tripwalaah trip members (live locations + trip events).
/// Clients connect to <c>/hubs/trip</c> and join a trip group.
/// </summary>
public sealed class TripHub(
    ITripPresenceStore presenceStore,
    ITripLiveUpdateService liveUpdateService,
    ILogger<TripHub> logger) : Hub
{
    public async Task JoinTrip(JoinTripRequest request)
    {
        ValidateJoin(request);

        var tripId = request.TripId.Trim();
        var userId = request.UserId.Trim();
        var group = TripHubGroups.ForTrip(tripId);

        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        await presenceStore.AddOrUpdateAsync(
            tripId,
            userId,
            Context.ConnectionId,
            request.DisplayName,
            Context.ConnectionAborted);

        var memberEvent = new TripMemberEventDto(
            tripId,
            userId,
            request.DisplayName,
            DateTime.UtcNow);

        await liveUpdateService.NotifyMemberJoinedAsync(memberEvent, Context.ConnectionAborted);
        await liveUpdateService.SendPresenceSnapshotAsync(tripId, Context.ConnectionId, Context.ConnectionAborted);

        logger.LogInformation(
            "User {UserId} joined trip {TripId} on connection {ConnectionId}",
            userId,
            tripId,
            Context.ConnectionId);
    }

    public async Task LeaveTrip(string tripId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tripId);
        tripId = tripId.Trim();

        var group = TripHubGroups.ForTrip(tripId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);

        var removed = await presenceStore.RemoveByConnectionAsync(Context.ConnectionId, Context.ConnectionAborted);
        if (removed is not null)
        {
            await liveUpdateService.NotifyMemberLeftAsync(
                new TripMemberEventDto(removed.TripId, removed.UserId, removed.DisplayName, DateTime.UtcNow),
                Context.ConnectionAborted);
        }

        logger.LogInformation(
            "Connection {ConnectionId} left trip {TripId}",
            Context.ConnectionId,
            tripId);
    }

    public async Task UpdateLocation(LiveLocationUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TripId);
        ValidateCoordinates(request.Latitude, request.Longitude);

        var tripId = request.TripId.Trim();
        var userId = await presenceStore.GetUserIdByConnectionAsync(Context.ConnectionId, Context.ConnectionAborted);
        if (string.IsNullOrWhiteSpace(userId))
        {
            await Clients.Caller.SendAsync(
                TripHubClientEvents.Error,
                new { error = "Join a trip before sending location updates." },
                Context.ConnectionAborted);
            return;
        }

        var update = await presenceStore.UpdateLocationAsync(
            tripId,
            userId,
            request.Latitude,
            request.Longitude,
            request.SpeedKmh,
            request.Heading,
            Context.ConnectionAborted);

        if (update is null)
        {
            await Clients.Caller.SendAsync(
                TripHubClientEvents.Error,
                new { error = "You are not an active member of this trip." },
                Context.ConnectionAborted);
            return;
        }

        await liveUpdateService.BroadcastLocationAsync(update, Context.ConnectionAborted);
    }

    public async Task GetPresence(string tripId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tripId);
        await liveUpdateService.SendPresenceSnapshotAsync(
            tripId.Trim(),
            Context.ConnectionId,
            Context.ConnectionAborted);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var removed = await presenceStore.RemoveByConnectionAsync(Context.ConnectionId);
        if (removed is not null)
        {
            await liveUpdateService.NotifyMemberLeftAsync(
                new TripMemberEventDto(removed.TripId, removed.UserId, removed.DisplayName, DateTime.UtcNow));
        }

        await base.OnDisconnectedAsync(exception);
    }

    private static void ValidateJoin(JoinTripRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TripId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);
    }

    private static void ValidateCoordinates(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new HubException("Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new HubException("Longitude must be between -180 and 180.");
        }
    }
}
