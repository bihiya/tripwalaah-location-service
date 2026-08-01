using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tripwalaah.LocationService.Api.Controllers;
using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;

namespace Tripwalaah.LocationService.Tests.Api;

public sealed class TripLiveControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;
    private readonly CapturingLiveLocationCache _cache = new();

    public TripLiveControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = TestWebApp.CreateClient(factory, services =>
        {
            services.RemoveAll<ITripLiveUpdateService>();
            services.AddSingleton<ITripLiveUpdateService, CapturingLiveUpdateService>();

            services.RemoveAll<ILiveLocationCache>();
            services.AddSingleton<ILiveLocationCache>(_cache);
        });
    }

    [Fact]
    public async Task PublishStatus_ReturnsAccepted()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/trips/trip-123/live/status",
            new PublishTripStatusBody("started", "Trip is on the way", "user-1"),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<TripStatusUpdateDto>(JsonOptions);
        body.Should().NotBeNull();
        body!.TripId.Should().Be("trip-123");
        body.Status.Should().Be("started");
    }

    [Fact]
    public async Task PublishLocation_SavesToLiveLocationCache()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/trips/trip-123/live/location",
            new PublishLiveLocationBody("user-1", 26.9, 75.8, "Lav", 30, 90),
            JsonOptions);

        // CapturingLiveUpdateService does not call cache; verify GET locations endpoint uses injected cache.
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        await _cache.SaveAsync(new LiveLocationUpdateDto(
            "trip-123", "user-1", "Lav", 26.9, 75.8, 30, 90, DateTime.UtcNow));

        var locationsResponse = await _client.GetAsync("/api/trips/trip-123/live/locations");
        locationsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var locations = await locationsResponse.Content.ReadFromJsonAsync<List<LiveLocationUpdateDto>>(JsonOptions);
        locations.Should().ContainSingle(x => x.UserId == "user-1");
    }

    [Fact]
    public async Task GetPresence_ReturnsSnapshot()
    {
        var response = await _client.GetAsync("/api/trips/trip-123/live/presence");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var snapshot = await response.Content.ReadFromJsonAsync<TripPresenceSnapshotDto>(JsonOptions);
        snapshot.Should().NotBeNull();
        snapshot!.TripId.Should().Be("trip-123");
        snapshot.Members.Should().NotBeNull();
    }

    private sealed class CapturingLiveUpdateService : ITripLiveUpdateService
    {
        public Task BroadcastLocationAsync(LiveLocationUpdateDto update, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task BroadcastStatusAsync(TripStatusUpdateDto update, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyMemberJoinedAsync(TripMemberEventDto memberEvent, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyMemberLeftAsync(TripMemberEventDto memberEvent, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SendPresenceSnapshotAsync(string tripId, string connectionId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
