using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Infrastructure.Persistence;
using Tripwalaah.LocationService.Infrastructure.Realtime;

namespace Tripwalaah.LocationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(options =>
        {
            configuration.GetSection(MongoDbSettings.SectionName).Bind(options);

            // Align with Tripwalaah Node env var names when present.
            var mongoUri = configuration["MONGODB_URI"]
                ?? configuration.GetConnectionString("MongoDb")
                ?? options.ConnectionString;

            options.ConnectionString = mongoUri;

            if (int.TryParse(configuration["DB_MAX_POOL_SIZE"], out var maxPool))
            {
                options.MaxPoolSize = maxPool;
            }

            if (int.TryParse(configuration["DB_MIN_POOL_SIZE"], out var minPool))
            {
                options.MinPoolSize = minPool;
            }

            if (int.TryParse(configuration["DB_CONNECT_TIMEOUT"], out var connectTimeout))
            {
                options.ConnectTimeoutMs = connectTimeout;
            }

            if (int.TryParse(configuration["DB_SOCKET_TIMEOUT"], out var socketTimeout))
            {
                options.SocketTimeoutMs = socketTimeout;
            }

            if (string.IsNullOrWhiteSpace(options.DatabaseName))
            {
                options.DatabaseName = MongoUrl.Create(options.ConnectionString).DatabaseName
                    ?? "tripwalaah";
            }
        });

        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            var mongoUrl = MongoUrl.Create(settings.ConnectionString);
            var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
            clientSettings.MaxConnectionPoolSize = settings.MaxPoolSize;
            clientSettings.MinConnectionPoolSize = settings.MinPoolSize;
            clientSettings.ConnectTimeout = TimeSpan.FromMilliseconds(settings.ConnectTimeoutMs);
            clientSettings.SocketTimeout = TimeSpan.FromMilliseconds(settings.SocketTimeoutMs);
            return new MongoClient(clientSettings);
        });

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            var databaseName = settings.DatabaseName;
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                databaseName = MongoUrl.Create(settings.ConnectionString).DatabaseName ?? "tripwalaah";
            }

            return client.GetDatabase(databaseName);
        });

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            var database = sp.GetRequiredService<IMongoDatabase>();
            return database.GetCollection<LocationDocument>(settings.LocationsCollectionName);
        });

        services.AddScoped<ILocationRepository, LocationMongoRepository>();
        services.AddSingleton<ITripPresenceStore, InMemoryTripPresenceStore>();
        services.AddHostedService<LocationSeedHostedService>();

        services.AddHealthChecks()
            .AddMongoDb(sp => sp.GetRequiredService<IMongoClient>(), name: "mongodb");

        return services;
    }
}
