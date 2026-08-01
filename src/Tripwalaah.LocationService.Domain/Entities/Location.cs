namespace Tripwalaah.LocationService.Domain.Entities;

public sealed class Location
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;
    public string? Region { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public LocationType Type { get; private set; }
    public string? Description { get; private set; }
    public string? Timezone { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

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
        string? region = null,
        string? description = null,
        string? timezone = null)
    {
        ValidateCoordinates(latitude, longitude);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);

        return new Location
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            City = city.Trim(),
            Country = country.Trim(),
            CountryCode = countryCode.Trim().ToUpperInvariant(),
            Region = string.IsNullOrWhiteSpace(region) ? null : region.Trim(),
            Latitude = latitude,
            Longitude = longitude,
            Type = type,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Timezone = string.IsNullOrWhiteSpace(timezone) ? null : timezone.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string name,
        string city,
        string country,
        string countryCode,
        double latitude,
        double longitude,
        LocationType type,
        string? region = null,
        string? description = null,
        string? timezone = null)
    {
        ValidateCoordinates(latitude, longitude);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);

        Name = name.Trim();
        City = city.Trim();
        Country = country.Trim();
        CountryCode = countryCode.Trim().ToUpperInvariant();
        Region = string.IsNullOrWhiteSpace(region) ? null : region.Trim();
        Latitude = latitude;
        Longitude = longitude;
        Type = type;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Timezone = string.IsNullOrWhiteSpace(timezone) ? null : timezone.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateCoordinates(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }
    }
}
