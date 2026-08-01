using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Domain.Entities;
using Tripwalaah.LocationService.Infrastructure.Messaging;
using Tripwalaah.LocationService.Infrastructure.Redis;

namespace Tripwalaah.LocationService.Tests.Api;

internal static class TestWebApp
{
    public static HttpClient CreateClient(
        WebApplicationFactory<Program> factory,
        Action<IServiceCollection>? configure = null)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("PORT", "0");
            builder.UseSetting("REDIS_ENABLED", "false");
            builder.UseSetting("KAFKA_ENABLED", "false");
            builder.UseSetting("Redis:Enabled", "false");
            builder.UseSetting("Kafka:Enabled", "false");

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILocationRepository>();
                services.AddSingleton<ILocationRepository, NoopLocationRepository>();

                services.RemoveAll<ILiveLocationCache>();
                services.AddSingleton<ILiveLocationCache, NullLiveLocationCache>();

                services.RemoveAll<ITripEventPublisher>();
                services.AddSingleton<ITripEventPublisher, NullTripEventPublisher>();

                var seed = services.FirstOrDefault(d =>
                    d.ImplementationType?.Name == "LocationSeedHostedService");
                if (seed is not null)
                {
                    services.Remove(seed);
                }

                foreach (var descriptor in services
                             .Where(d => d.ImplementationType?.Name.Contains("Kafka", StringComparison.Ordinal) == true)
                             .ToList())
                {
                    services.Remove(descriptor);
                }

                configure?.Invoke(services);
            });
        }).CreateClient();
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

internal sealed class CapturingLiveLocationCache : ILiveLocationCache
{
    public List<LiveLocationUpdateDto> Saved { get; } = [];

    public Task SaveAsync(LiveLocationUpdateDto update, CancellationToken cancellationToken = default)
    {
        Saved.RemoveAll(x => x.TripId == update.TripId && x.UserId == update.UserId);
        Saved.Add(update);
        return Task.CompletedTask;
    }

    public Task<LiveLocationUpdateDto?> GetAsync(
        string tripId,
        string userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Saved.FirstOrDefault(x => x.TripId == tripId && x.UserId == userId));

    public Task<IReadOnlyList<LiveLocationUpdateDto>> GetTripLocationsAsync(
        string tripId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LiveLocationUpdateDto>>(
            Saved.Where(x => x.TripId == tripId).ToList());

    public Task RemoveAsync(
        string tripId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        Saved.RemoveAll(x => x.TripId == tripId && x.UserId == userId);
        return Task.CompletedTask;
    }
}
