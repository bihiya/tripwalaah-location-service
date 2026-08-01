namespace Tripwalaah.LocationService.Application.Realtime;

/// <summary>Client method names invoked by the TripHub.</summary>
public static class TripHubClientEvents
{
    public const string MemberJoined = "MemberJoined";
    public const string MemberLeft = "MemberLeft";
    public const string LocationUpdated = "LocationUpdated";
    public const string TripStatusUpdated = "TripStatusUpdated";
    public const string PresenceSnapshot = "PresenceSnapshot";
    public const string Error = "Error";
}
