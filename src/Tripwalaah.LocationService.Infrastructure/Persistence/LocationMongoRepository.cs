using MongoDB.Bson;
using MongoDB.Driver;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Domain.Entities;

namespace Tripwalaah.LocationService.Infrastructure.Persistence;

public sealed class LocationMongoRepository(IMongoCollection<LocationDocument> collection) : ILocationRepository
{
    public async Task<Location?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        var document = await collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<(IReadOnlyList<Location> Items, int TotalCount)> SearchAsync(
        string? query,
        string? countryCode,
        string? city,
        LocationType? type,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<LocationDocument>>();
        var builder = Builders<LocationDocument>.Filter;

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            filters.Add(builder.Or(
                builder.Regex(x => x.Name, new BsonRegularExpression(term, "i")),
                builder.Regex(x => x.City, new BsonRegularExpression(term, "i")),
                builder.Regex(x => x.Country, new BsonRegularExpression(term, "i")),
                builder.Regex(x => x.Region!, new BsonRegularExpression(term, "i")),
                builder.Regex(x => x.State!, new BsonRegularExpression(term, "i"))));
        }

        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            filters.Add(builder.Eq(x => x.CountryCode, countryCode.Trim().ToUpperInvariant()));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            filters.Add(builder.Regex(x => x.City, new BsonRegularExpression($"^{city.Trim()}$", "i")));
        }

        if (type is not null)
        {
            filters.Add(builder.Eq(x => x.Type, type));
        }

        if (isActive is not null)
        {
            filters.Add(builder.Eq(x => x.IsActive, isActive));
        }

        var filter = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var totalCount = (int)await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var items = await collection
            .Find(filter)
            .SortBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (items.Select(x => x.ToDomain()).ToList(), totalCount);
    }

    public async Task AddAsync(Location location, CancellationToken cancellationToken = default)
    {
        var document = LocationDocument.FromDomain(location);
        await collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        location.AssignId(document.Id);
    }

    public async Task UpdateAsync(Location location, CancellationToken cancellationToken = default)
    {
        var document = LocationDocument.FromDomain(location);
        await collection.ReplaceOneAsync(
            x => x.Id == document.Id,
            document,
            cancellationToken: cancellationToken);
    }
}
