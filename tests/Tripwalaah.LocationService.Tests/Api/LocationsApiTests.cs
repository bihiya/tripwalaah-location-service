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
using Microsoft.Extensions.Hosting;
using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Domain.Entities;

namespace Tripwalaah.LocationService.Tests.Api;

public sealed class LocationsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public LocationsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("PORT", "0");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILocationRepository>();
                services.AddSingleton<ILocationRepository, FakeLocationRepository>();

                // Avoid Mongo seed during controller tests.
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
    public async Task CreateAndGetLocation_ReturnsCreatedLocation()
    {
        var request = new CreateLocationRequest(
            "Gateway of India",
            "Mumbai",
            "India",
            "IN",
            18.921984,
            72.834654,
            LocationType.Landmark,
            "Maharashtra",
            "Maharashtra",
            "Iconic waterfront monument");

        var createResponse = await _client.PostAsJsonAsync("/api/locations", request, JsonOptions);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<LocationResponse>(JsonOptions);
        created.Should().NotBeNull();
        created!.Name.Should().Be("Gateway of India");

        var getResponse = await _client.GetAsync($"/api/locations/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<LocationResponse>(JsonOptions);
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
        fetched.CountryCode.Should().Be("IN");
    }

    [Fact]
    public async Task SearchLocations_ReturnsPagedResults()
    {
        var response = await _client.GetAsync("/api/locations?countryCode=IN");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<LocationResponse>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    private sealed class FakeLocationRepository : ILocationRepository
    {
        private readonly List<Location> _locations = [];
        private int _counter;

        public Task<Location?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_locations.FirstOrDefault(x => x.Id == id));

        public Task<(IReadOnlyList<Location> Items, int TotalCount)> SearchAsync(
            string? query,
            string? countryCode,
            string? city,
            LocationType? type,
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Location> queryable = _locations;

            if (!string.IsNullOrWhiteSpace(countryCode))
            {
                var code = countryCode.Trim().ToUpperInvariant();
                queryable = queryable.Where(x => x.CountryCode == code);
            }

            if (isActive is not null)
            {
                queryable = queryable.Where(x => x.IsActive == isActive);
            }

            var filtered = queryable.OrderBy(x => x.Name).ToList();
            var pageItems = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(((IReadOnlyList<Location>)pageItems, filtered.Count));
        }

        public Task AddAsync(Location location, CancellationToken cancellationToken = default)
        {
            _counter++;
            // Valid-looking ObjectId hex length
            location.AssignId($"507f1f77bcf86cd79943{_counter:D4}");
            _locations.Add(location);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Location location, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
