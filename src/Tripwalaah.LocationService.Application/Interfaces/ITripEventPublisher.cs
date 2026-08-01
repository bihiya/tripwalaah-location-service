using Tripwalaah.LocationService.Application.DTOs;

namespace Tripwalaah.LocationService.Application.Interfaces;

public interface ITripEventPublisher
{
    Task PublishLocationUpdatedAsync(
        LiveLocationUpdateDto update,
        CancellationToken cancellationToken = default);

    Task PublishTripStatusUpdatedAsync(
        TripStatusUpdateDto update,
        CancellationToken cancellationToken = default);

    Task PublishMemberJoinedAsync(
        TripMemberEventDto memberEvent,
        CancellationToken cancellationToken = default);

    Task PublishMemberLeftAsync(
        TripMemberEventDto memberEvent,
        CancellationToken cancellationToken = default);
}
