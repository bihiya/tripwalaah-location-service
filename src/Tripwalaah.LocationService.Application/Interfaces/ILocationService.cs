using Tripwalaah.LocationService.Application.DTOs;

namespace Tripwalaah.LocationService.Application.Interfaces;

public interface ILocationService
{
    Task<LocationResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<PagedResult<LocationResponse>> SearchAsync(
        LocationSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<LocationResponse> CreateAsync(
        CreateLocationRequest request,
        CancellationToken cancellationToken = default);

    Task<LocationResponse?> UpdateAsync(
        string id,
        UpdateLocationRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(string id, CancellationToken cancellationToken = default);
}
