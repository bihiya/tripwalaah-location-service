using Microsoft.AspNetCore.Mvc;
using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Domain.Entities;

namespace Tripwalaah.LocationService.Api.Controllers;

[ApiController]
[Route("api/locations")]
public sealed class LocationsController(ILocationService locationService) : ControllerBase
{
    /// <summary>Search locations by query, country, city, or type.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LocationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LocationResponse>>> Search(
        [FromQuery] string? query,
        [FromQuery] string? countryCode,
        [FromQuery] string? city,
        [FromQuery] LocationType? type,
        [FromQuery] bool? isActive = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await locationService.SearchAsync(
            new LocationSearchRequest(query, countryCode, city, type, isActive, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>Get a location by MongoDB ObjectId.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationResponse>> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        var location = await locationService.GetByIdAsync(id, cancellationToken);
        return location is null ? NotFound() : Ok(location);
    }

    /// <summary>Create a new location document.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LocationResponse>> Create(
        [FromBody] CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await locationService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Update an existing location.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LocationResponse>> Update(
        string id,
        [FromBody] UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await locationService.UpdateAsync(id, request, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Soft-delete (deactivate) a location.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(string id, CancellationToken cancellationToken)
    {
        var deactivated = await locationService.DeactivateAsync(id, cancellationToken);
        return deactivated ? NoContent() : NotFound();
    }
}
