using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Tripwalaah.LocationService.Api.Hubs;
using Tripwalaah.LocationService.Api.Realtime;
using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Tests.Api;

namespace Tripwalaah.LocationService.Tests.Realtime;

public sealed class TripLiveUpdateServiceTests
{
    [Fact]
    public async Task BroadcastLocationAsync_SavesToRedisCacheAndPublishesKafkaEvent()
    {
        var cache = new CapturingLiveLocationCache();
        var publisher = new CapturingTripEventPublisher();
        var hubContext = CreateHubContextMock();
        var presence = new Mock<ITripPresenceStore>();

        var service = new TripLiveUpdateService(
            hubContext.Object,
            presence.Object,
            cache,
            publisher);

        var update = new LiveLocationUpdateDto(
            "trip-1",
            "user-1",
            "Lav",
            26.9,
            75.8,
            40,
            180,
            DateTime.UtcNow);

        await service.BroadcastLocationAsync(update);

        cache.Saved.Should().ContainSingle(x => x.UserId == "user-1" && x.TripId == "trip-1");
        publisher.Locations.Should().ContainSingle(x => x.UserId == "user-1");
    }

    private static Mock<IHubContext<TripHub>> CreateHubContextMock()
    {
        var clients = new Mock<IHubClients>();
        var clientProxy = new Mock<IClientProxy>();
        var singleClientProxy = clientProxy.As<ISingleClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        clients.Setup(x => x.Group(It.IsAny<string>())).Returns(clientProxy.Object);
        clients.Setup(x => x.Client(It.IsAny<string>())).Returns(singleClientProxy.Object);

        var hubContext = new Mock<IHubContext<TripHub>>();
        hubContext.SetupGet(x => x.Clients).Returns(clients.Object);
        return hubContext;
    }

    private sealed class CapturingTripEventPublisher : ITripEventPublisher
    {
        public List<LiveLocationUpdateDto> Locations { get; } = [];

        public Task PublishLocationUpdatedAsync(LiveLocationUpdateDto update, CancellationToken cancellationToken = default)
        {
            Locations.Add(update);
            return Task.CompletedTask;
        }

        public Task PublishTripStatusUpdatedAsync(TripStatusUpdateDto update, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PublishMemberJoinedAsync(TripMemberEventDto memberEvent, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task PublishMemberLeftAsync(TripMemberEventDto memberEvent, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
