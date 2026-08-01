using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Tripwalaah.LocationService.Domain.Entities;

namespace Tripwalaah.LocationService.Infrastructure.Persistence;

public sealed class LocationSeedHostedService(
    IServiceProvider serviceProvider,
    IOptions<MongoDbSettings> settings,
    IHostEnvironment environment,
    ILogger<LocationSeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var collection = scope.ServiceProvider.GetRequiredService<IMongoCollection<LocationDocument>>();

        await EnsureIndexesAsync(collection, cancellationToken);

        if (!environment.IsDevelopment())
        {
            return;
        }

        var count = await collection.CountDocumentsAsync(FilterDefinition<LocationDocument>.Empty, cancellationToken: cancellationToken);
        if (count > 0)
        {
            return;
        }

        logger.LogInformation(
            "Seeding sample locations into {Database}.{Collection}",
            settings.Value.DatabaseName,
            settings.Value.LocationsCollectionName);

        var seed = new[]
        {
            Location.Create("Indira Gandhi International Airport", "New Delhi", "India", "IN",
                28.5562, 77.1000, LocationType.Airport, "Delhi", "Delhi NCR",
                "Primary international gateway for Delhi.", "Asia/Kolkata"),
            Location.Create("Jaipur", "Jaipur", "India", "IN",
                26.9124, 75.7873, LocationType.City, "Rajasthan", "Rajasthan",
                "Pink City and heritage destination.", "Asia/Kolkata"),
            Location.Create("Gateway of India", "Mumbai", "India", "IN",
                18.921984, 72.834654, LocationType.Landmark, "Maharashtra", "Maharashtra",
                "Iconic waterfront monument.", "Asia/Kolkata"),
            Location.Create("Changi Airport", "Singapore", "Singapore", "SG",
                1.3644, 103.9915, LocationType.Airport, null, null,
                "Major Southeast Asia hub.", "Asia/Singapore"),
            Location.Create("Dubai", "Dubai", "United Arab Emirates", "AE",
                25.2048, 55.2708, LocationType.City, "Dubai", "Dubai",
                "Global travel and leisure hub.", "Asia/Dubai")
        }.Select(LocationDocument.FromDomain);

        await collection.InsertManyAsync(seed, cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureIndexesAsync(
        IMongoCollection<LocationDocument> collection,
        CancellationToken cancellationToken)
    {
        var indexes = new[]
        {
            new CreateIndexModel<LocationDocument>(
                Builders<LocationDocument>.IndexKeys.Ascending(x => x.CountryCode)),
            new CreateIndexModel<LocationDocument>(
                Builders<LocationDocument>.IndexKeys.Ascending(x => x.City)),
            new CreateIndexModel<LocationDocument>(
                Builders<LocationDocument>.IndexKeys.Ascending(x => x.Name)),
            new CreateIndexModel<LocationDocument>(
                Builders<LocationDocument>.IndexKeys.Ascending(x => x.Type)),
            new CreateIndexModel<LocationDocument>(
                Builders<LocationDocument>.IndexKeys.Geo2DSphere(x => x.Location))
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
    }
}
