using Tripwalaah.LocationService.Application.DTOs;

namespace Tripwalaah.LocationService.Application.Interfaces;

public interface ITripLiveUpdateService
{
    Task BroadcastLocationAsync(
        LiveLocationUpdateDto update,
        CancellationToken cancellationToken = default);

    Task BroadcastStatusAsync(
        TripStatusUpdateDto update,
        CancellationToken cancellationToken = default);

    Task NotifyMemberJoinedAsync(
        TripMemberEventDto memberEvent,
        CancellationToken cancellationToken = default);

    Task NotifyMemberLeftAsync(
        TripMemberEventDto memberEvent,
        CancellationToken cancellationToken = default);

    Task SendPresenceSnapshotAsync(
        string tripId,
        string connectionId,
        CancellationToken cancellationToken = default);
}
