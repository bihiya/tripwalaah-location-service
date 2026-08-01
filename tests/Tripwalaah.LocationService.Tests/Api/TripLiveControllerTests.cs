using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tripwalaah.LocationService.Api.Controllers;
using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Domain.Entities;

namespace Tripwalaah.LocationService.Tests.Api;

public sealed class TripLiveControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public TripLiveControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("PORT", "0");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILocationRepository>();
                services.AddSingleton<ILocationRepository, NoopLocationRepository>();

                services.RemoveAll<ITripLiveUpdateService>();
                services.AddSingleton<ITripLiveUpdateService, CapturingLiveUpdateService>();

                var seed = services.FirstOrDefault(d =>
                    d.ImplementationType?.Name == "LocationSeedHostedService");
                if (seed is not null)
                {
                    services.Remove(seed);
                }
            });
        }).CreateClient();
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

    private sealed class NoopLocationRepository : ILocationRepository
    {
        public Task<Location?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Location?>(null);

        public Task<(IReadOnlyList<Location> Items, int TotalCount)> SearchAsync(
            string? query,
            string? countryCode,
            string? city,
            LocationType? type,
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(((IReadOnlyList<Location>)[], 0));

        public Task AddAsync(Location location, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(Location location, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
