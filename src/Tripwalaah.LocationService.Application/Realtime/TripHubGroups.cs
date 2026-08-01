namespace Tripwalaah.LocationService.Application.Realtime;

public static class TripHubGroups
{
    public static string ForTrip(string tripId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tripId);
        return $"trip:{tripId.Trim()}";
    }
}
