using Tripwalaah.LocationService.Domain.Entities;

namespace Tripwalaah.LocationService.Application.DTOs;

public sealed record LocationResponse(
    string Id,
    string Name,
    string City,
    string? State,
    string Country,
    string CountryCode,
    string? Region,
    double Latitude,
    double Longitude,
    LocationType Type,
    string? Description,
    string? Timezone,
    string? GooglePlaceId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateLocationRequest(
    string Name,
    string City,
    string Country,
    string CountryCode,
    double Latitude,
    double Longitude,
    LocationType Type,
    string? State = null,
    string? Region = null,
    string? Description = null,
    string? Timezone = null,
    string? GooglePlaceId = null);

public sealed record UpdateLocationRequest(
    string Name,
    string City,
    string Country,
    string CountryCode,
    double Latitude,
    double Longitude,
    LocationType Type,
    string? State = null,
    string? Region = null,
    string? Description = null,
    string? Timezone = null,
    string? GooglePlaceId = null);

public sealed record LocationSearchRequest(
    string? Query = null,
    string? CountryCode = null,
    string? City = null,
    LocationType? Type = null,
    bool? IsActive = true,
    int Page = 1,
    int PageSize = 20);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
