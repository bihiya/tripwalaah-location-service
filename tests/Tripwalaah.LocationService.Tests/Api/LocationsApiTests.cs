using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Tripwalaah.LocationService.Application.DTOs;
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
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:Provider"] = "InMemory",
                    ["ConnectionStrings:LocationDb"] = $"TripwalaahLocationsTests-{Guid.NewGuid()}"
                });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
            "Iconic waterfront monument");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/locations", request, JsonOptions);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<LocationResponse>(JsonOptions);
        created.Should().NotBeNull();
        created!.Name.Should().Be("Gateway of India");

        var getResponse = await _client.GetAsync($"/api/v1/locations/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<LocationResponse>(JsonOptions);
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
        fetched.CountryCode.Should().Be("IN");
    }

    [Fact]
    public async Task SearchLocations_ReturnsPagedResults()
    {
        var response = await _client.GetAsync("/api/v1/locations?countryCode=IN");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<LocationResponse>>(JsonOptions);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }
}
