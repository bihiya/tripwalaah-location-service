using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Domain.Entities;

namespace Tripwalaah.LocationService.Api.Endpoints;

public static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/locations")
            .WithTags("Locations");

        group.MapGet("/", SearchLocations)
            .WithName("SearchLocations")
            .WithSummary("Search locations by query, country, or type");

        group.MapGet("/{id:guid}", GetLocationById)
            .WithName("GetLocationById")
            .WithSummary("Get a location by id");

        group.MapPost("/", CreateLocation)
            .WithName("CreateLocation")
            .WithSummary("Create a new location");

        group.MapPut("/{id:guid}", UpdateLocation)
            .WithName("UpdateLocation")
            .WithSummary("Update an existing location");

        group.MapDelete("/{id:guid}", DeactivateLocation)
            .WithName("DeactivateLocation")
            .WithSummary("Soft-delete (deactivate) a location");

        return app;
    }

    private static async Task<IResult> SearchLocations(
        ILocationService locationService,
        string? query,
        string? countryCode,
        LocationType? type,
        bool? isActive,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await locationService.SearchAsync(
            new LocationSearchRequest(query, countryCode, type, isActive, page, pageSize),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetLocationById(
        Guid id,
        ILocationService locationService,
        CancellationToken cancellationToken)
    {
        var location = await locationService.GetByIdAsync(id, cancellationToken);
        return location is null ? Results.NotFound() : Results.Ok(location);
    }

    private static async Task<IResult> CreateLocation(
        CreateLocationRequest request,
        ILocationService locationService,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await locationService.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/v1/locations/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UpdateLocation(
        Guid id,
        UpdateLocationRequest request,
        ILocationService locationService,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await locationService.UpdateAsync(id, request, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeactivateLocation(
        Guid id,
        ILocationService locationService,
        CancellationToken cancellationToken)
    {
        var deactivated = await locationService.DeactivateAsync(id, cancellationToken);
        return deactivated ? Results.NoContent() : Results.NotFound();
    }
}
