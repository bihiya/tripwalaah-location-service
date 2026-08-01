using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Tripwalaah.LocationService.Domain.Entities;

namespace Tripwalaah.LocationService.Infrastructure.Persistence;

/// <summary>
/// MongoDB document mapped to the Tripwalaah `locations` collection.
/// Uses GeoJSON Point coordinates like the Node/Mongoose models.
/// </summary>
public sealed class LocationDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    [BsonElement("state")]
    [BsonIgnoreIfNull]
    public string? State { get; set; }

    [BsonElement("country")]
    public string Country { get; set; } = string.Empty;

    [BsonElement("countryCode")]
    public string CountryCode { get; set; } = string.Empty;

    [BsonElement("region")]
    [BsonIgnoreIfNull]
    public string? Region { get; set; }

    [BsonElement("location")]
    public GeoJsonPointDocument Location { get; set; } = new();

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public LocationType Type { get; set; }

    [BsonElement("description")]
    [BsonIgnoreIfNull]
    public string? Description { get; set; }

    [BsonElement("timezone")]
    [BsonIgnoreIfNull]
    public string? Timezone { get; set; }

    [BsonElement("googlePlaceId")]
    [BsonIgnoreIfNull]
    public string? GooglePlaceId { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    public static LocationDocument FromDomain(Location location) =>
        new()
        {
            Id = string.IsNullOrWhiteSpace(location.Id) || !ObjectId.TryParse(location.Id, out _)
                ? ObjectId.GenerateNewId().ToString()
                : location.Id,
            Name = location.Name,
            City = location.City,
            State = location.State,
            Country = location.Country,
            CountryCode = location.CountryCode,
            Region = location.Region,
            Location = new GeoJsonPointDocument
            {
                Type = "Point",
                Coordinates = [location.Coordinates.Longitude, location.Coordinates.Latitude]
            },
            Type = location.Type,
            Description = location.Description,
            Timezone = location.Timezone,
            GooglePlaceId = location.GooglePlaceId,
            IsActive = location.IsActive,
            CreatedAt = location.CreatedAt,
            UpdatedAt = location.UpdatedAt
        };

    public Location ToDomain() =>
        Domain.Entities.Location.Rehydrate(
            Id,
            Name,
            City,
            State,
            Country,
            CountryCode,
            Region,
            Location.Latitude,
            Location.Longitude,
            Type,
            Description,
            Timezone,
            GooglePlaceId,
            IsActive,
            CreatedAt,
            UpdatedAt);
}

public sealed class GeoJsonPointDocument
{
    [BsonElement("type")]
    public string Type { get; set; } = "Point";

    /// <summary>GeoJSON order: [longitude, latitude].</summary>
    [BsonElement("coordinates")]
    public double[] Coordinates { get; set; } = [0, 0];

    [BsonIgnore]
    public double Longitude => Coordinates.Length > 0 ? Coordinates[0] : 0;

    [BsonIgnore]
    public double Latitude => Coordinates.Length > 1 ? Coordinates[1] : 0;
}
