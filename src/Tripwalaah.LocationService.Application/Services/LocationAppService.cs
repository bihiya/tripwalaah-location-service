using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Domain.Entities;

namespace Tripwalaah.LocationService.Application.Services;

public sealed class LocationAppService(ILocationRepository repository) : ILocationService
{
    public async Task<LocationResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var location = await repository.GetByIdAsync(id, cancellationToken);
        return location is null ? null : Map(location);
    }

    public async Task<PagedResult<LocationResponse>> SearchAsync(
        LocationSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var (items, totalCount) = await repository.SearchAsync(
            request.Query,
            request.CountryCode,
            request.City,
            request.Type,
            request.IsActive,
            page,
            pageSize,
            cancellationToken);

        return new PagedResult<LocationResponse>(
            items.Select(Map).ToList(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<LocationResponse> CreateAsync(
        CreateLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var location = Location.Create(
            request.Name,
            request.City,
            request.Country,
            request.CountryCode,
            request.Latitude,
            request.Longitude,
            request.Type,
            request.State,
            request.Region,
            request.Description,
            request.Timezone,
            request.GooglePlaceId);

        await repository.AddAsync(location, cancellationToken);
        return Map(location);
    }

    public async Task<LocationResponse?> UpdateAsync(
        string id,
        UpdateLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var location = await repository.GetByIdAsync(id, cancellationToken);
        if (location is null)
        {
            return null;
        }

        location.Update(
            request.Name,
            request.City,
            request.Country,
            request.CountryCode,
            request.Latitude,
            request.Longitude,
            request.Type,
            request.State,
            request.Region,
            request.Description,
            request.Timezone,
            request.GooglePlaceId);

        await repository.UpdateAsync(location, cancellationToken);
        return Map(location);
    }

    public async Task<bool> DeactivateAsync(string id, CancellationToken cancellationToken = default)
    {
        var location = await repository.GetByIdAsync(id, cancellationToken);
        if (location is null)
        {
            return false;
        }

        location.Deactivate();
        await repository.UpdateAsync(location, cancellationToken);
        return true;
    }

    private static LocationResponse Map(Location location) =>
        new(
            location.Id,
            location.Name,
            location.City,
            location.State,
            location.Country,
            location.CountryCode,
            location.Region,
            location.Coordinates.Latitude,
            location.Coordinates.Longitude,
            location.Type,
            location.Description,
            location.Timezone,
            location.GooglePlaceId,
            location.IsActive,
            location.CreatedAt,
            location.UpdatedAt);
}
