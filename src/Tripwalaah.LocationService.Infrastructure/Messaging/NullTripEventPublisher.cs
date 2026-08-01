using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;

namespace Tripwalaah.LocationService.Infrastructure.Messaging;

public sealed class NullTripEventPublisher : ITripEventPublisher
{
    public Task PublishLocationUpdatedAsync(
        LiveLocationUpdateDto update,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task PublishTripStatusUpdatedAsync(
        TripStatusUpdateDto update,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task PublishMemberJoinedAsync(
        TripMemberEventDto memberEvent,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task PublishMemberLeftAsync(
        TripMemberEventDto memberEvent,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
