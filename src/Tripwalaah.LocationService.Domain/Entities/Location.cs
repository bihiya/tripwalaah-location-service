namespace Tripwalaah.LocationService.Domain.Entities;

/// <summary>
/// Location document shape aligned with Tripwalaah MongoDB collections.
/// </summary>
public sealed class Location
{
    public string Id { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string? State { get; private set; }
    public string Country { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;
    public string? Region { get; private set; }
    public GeoPoint Coordinates { get; private set; } = GeoPoint.Create(0, 0);
    public LocationType Type { get; private set; }
    public string? Description { get; private set; }
    public string? Timezone { get; private set; }
    public string? GooglePlaceId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Location()
    {
    }

    public static Location Create(
        string name,
        string city,
        string country,
        string countryCode,
        double latitude,
        double longitude,
        LocationType type,
        string? state = null,
        string? region = null,
        string? description = null,
        string? timezone = null,
        string? googlePlaceId = null,
        string? id = null)
    {
        Validate(name, city, country, countryCode, latitude, longitude);
        var now = DateTime.UtcNow;

        return new Location
        {
            Id = string.IsNullOrWhiteSpace(id) ? string.Empty : id,
            Name = name.Trim(),
            City = city.Trim(),
            State = NormalizeOptional(state),
            Country = country.Trim(),
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            Region = NormalizeOptional(region),
            Coordinates = GeoPoint.Create(longitude, latitude),
            Type = type,
            Description = NormalizeOptional(description),
            Timezone = NormalizeOptional(timezone),
            GooglePlaceId = NormalizeOptional(googlePlaceId),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void AssignId(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
    }

    public void Update(
        string name,
        string city,
        string country,
        string countryCode,
        double latitude,
        double longitude,
        LocationType type,
        string? state = null,
        string? region = null,
        string? description = null,
        string? timezone = null,
        string? googlePlaceId = null)
    {
        Validate(name, city, country, countryCode, latitude, longitude);

        Name = name.Trim();
        City = city.Trim();
        State = NormalizeOptional(state);
        Country = country.Trim();
        CountryCode = countryCode.Trim().ToUpperInvariant();
        Region = NormalizeOptional(region);
        Coordinates = GeoPoint.Create(longitude, latitude);
        Type = type;
        Description = NormalizeOptional(description);
        Timezone = NormalizeOptional(timezone);
        GooglePlaceId = NormalizeOptional(googlePlaceId);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    // Used by infrastructure when hydrating from MongoDB.
    public static Location Rehydrate(
        string id,
        string name,
        string city,
        string? state,
        string country,
        string countryCode,
        string? region,
        double latitude,
        double longitude,
        LocationType type,
        string? description,
        string? timezone,
        string? googlePlaceId,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new Location
        {
            Id = id,
            Name = name,
            City = city,
            State = state,
            Country = country,
            CountryCode = countryCode,
            Region = region,
            Coordinates = GeoPoint.Create(longitude, latitude),
            Type = type,
            Description = description,
            Timezone = timezone,
            GooglePlaceId = googlePlaceId,
            IsActive = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    private static void Validate(
        string name,
        string city,
        string country,
        string countryCode,
        double latitude,
        double longitude)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);

        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class GeoPoint
{
    public string Type { get; private set; } = "Point";
    public double[] Coordinates { get; private set; } = [0, 0];

    public double Longitude => Coordinates.Length > 0 ? Coordinates[0] : 0;
    public double Latitude => Coordinates.Length > 1 ? Coordinates[1] : 0;

    private GeoPoint()
    {
    }

    public static GeoPoint Create(double longitude, double latitude) =>
        new()
        {
            Type = "Point",
            Coordinates = [longitude, latitude]
        };
}
