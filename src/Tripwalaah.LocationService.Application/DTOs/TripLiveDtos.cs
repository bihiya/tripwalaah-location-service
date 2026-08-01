namespace Tripwalaah.LocationService.Application.DTOs;

public sealed record JoinTripRequest(
    string TripId,
    string UserId,
    string? DisplayName = null);

public sealed record LiveLocationUpdateRequest(
    string TripId,
    double Latitude,
    double Longitude,
    double? SpeedKmh = null,
    double? Heading = null);

public sealed record TripStatusUpdateRequest(
    string TripId,
    string Status,
    string? Message = null,
    string? TriggeredByUserId = null);

public sealed record LiveLocationUpdateDto(
    string TripId,
    string UserId,
    string? DisplayName,
    double Latitude,
    double Longitude,
    double? SpeedKmh,
    double? Heading,
    DateTime Timestamp);

public sealed record TripMemberPresenceDto(
    string TripId,
    string UserId,
    string? DisplayName,
    string ConnectionId,
    DateTime JoinedAt,
    LiveLocationUpdateDto? LastLocation);

public sealed record TripPresenceSnapshotDto(
    string TripId,
    IReadOnlyList<TripMemberPresenceDto> Members,
    DateTime Timestamp);

public sealed record TripMemberEventDto(
    string TripId,
    string UserId,
    string? DisplayName,
    DateTime Timestamp);

public sealed record TripStatusUpdateDto(
    string TripId,
    string Status,
    string? Message,
    string? TriggeredByUserId,
    DateTime Timestamp);
