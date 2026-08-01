using Microsoft.AspNetCore.Mvc;
using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;

namespace Tripwalaah.LocationService.Api.Controllers;

/// <summary>
/// REST endpoints for server-side trip live updates (e.g. Node API → broadcast via SignalR).
/// </summary>
[ApiController]
[Route("api/trips/{tripId}/live")]
public sealed class TripLiveController(
    ITripLiveUpdateService liveUpdateService,
    ITripPresenceStore presenceStore) : ControllerBase
{
    /// <summary>Broadcast a trip status change to all connected members.</summary>
    [HttpPost("status")]
    [ProducesResponseType(typeof(TripStatusUpdateDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TripStatusUpdateDto>> PublishStatus(
        string tripId,
        [FromBody] PublishTripStatusBody body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tripId) || string.IsNullOrWhiteSpace(body.Status))
        {
            return BadRequest(new { error = "tripId and status are required." });
        }

        var update = new TripStatusUpdateDto(
            tripId.Trim(),
            body.Status.Trim(),
            body.Message,
            body.TriggeredByUserId,
            DateTime.UtcNow);

        await liveUpdateService.BroadcastStatusAsync(update, cancellationToken);
        return Accepted(update);
    }

    /// <summary>Broadcast a member location update (server-side proxy).</summary>
    [HttpPost("location")]
    [ProducesResponseType(typeof(LiveLocationUpdateDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LiveLocationUpdateDto>> PublishLocation(
        string tripId,
        [FromBody] PublishLiveLocationBody body,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tripId) || string.IsNullOrWhiteSpace(body.UserId))
        {
            return BadRequest(new { error = "tripId and userId are required." });
        }

        if (body.Latitude is < -90 or > 90 || body.Longitude is < -180 or > 180)
        {
            return BadRequest(new { error = "Invalid coordinates." });
        }

        var update = new LiveLocationUpdateDto(
            tripId.Trim(),
            body.UserId.Trim(),
            body.DisplayName,
            body.Latitude,
            body.Longitude,
            body.SpeedKmh,
            body.Heading,
            DateTime.UtcNow);

        await liveUpdateService.BroadcastLocationAsync(update, cancellationToken);
        return Accepted(update);
    }

    /// <summary>Get currently connected members for a trip.</summary>
    [HttpGet("presence")]
    [ProducesResponseType(typeof(TripPresenceSnapshotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TripPresenceSnapshotDto>> GetPresence(
        string tripId,
        CancellationToken cancellationToken)
    {
        var members = await presenceStore.GetMembersAsync(tripId.Trim(), cancellationToken);
        return Ok(new TripPresenceSnapshotDto(tripId.Trim(), members, DateTime.UtcNow));
    }
}

public sealed record PublishTripStatusBody(
    string Status,
    string? Message = null,
    string? TriggeredByUserId = null);

public sealed record PublishLiveLocationBody(
    string UserId,
    double Latitude,
    double Longitude,
    string? DisplayName = null,
    double? SpeedKmh = null,
    double? Heading = null);
