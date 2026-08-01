using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using StackExchange.Redis;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Infrastructure.Messaging;
using Tripwalaah.LocationService.Infrastructure.Persistence;
using Tripwalaah.LocationService.Infrastructure.Realtime;
using Tripwalaah.LocationService.Infrastructure.Redis;

namespace Tripwalaah.LocationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ConfigureMongo(services, configuration);
        ConfigureRedis(services, configuration);
        ConfigureKafka(services, configuration);

        services.AddScoped<ILocationRepository, LocationMongoRepository>();
        services.AddSingleton<ITripPresenceStore, InMemoryTripPresenceStore>();
        services.AddHostedService<LocationSeedHostedService>();

        return services;
    }

    private static void ConfigureMongo(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(options =>
        {
            configuration.GetSection(MongoDbSettings.SectionName).Bind(options);

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

        services.AddHealthChecks()
            .AddMongoDb(sp => sp.GetRequiredService<IMongoClient>(), name: "mongodb");
    }

    private static void ConfigureRedis(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RedisSettings>(options =>
        {
            configuration.GetSection(RedisSettings.SectionName).Bind(options);

            var redisUrl = configuration["REDIS_URL"]
                ?? configuration.GetConnectionString("Redis")
                ?? options.ConnectionString;

            options.ConnectionString = NormalizeRedisConnection(redisUrl);

            if (bool.TryParse(configuration["REDIS_ENABLED"], out var enabled))
            {
                options.Enabled = enabled;
            }

            if (int.TryParse(configuration["REDIS_LIVE_LOCATION_TTL_SECONDS"], out var ttl))
            {
                options.LiveLocationTtlSeconds = ttl;
            }
        });

        var redisEnabled = configuration.GetValue("REDIS_ENABLED", configuration.GetValue("Redis:Enabled", true));
        if (!redisEnabled)
        {
            services.AddSingleton<ILiveLocationCache, NullLiveLocationCache>();
            return;
        }

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<RedisSettings>>().Value;
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Redis");

            var config = ConfigurationOptions.Parse(settings.ConnectionString);
            config.AbortOnConnectFail = false;
            config.ConnectRetry = 3;
            config.ConnectTimeout = 5000;

            var mux = ConnectionMultiplexer.Connect(config);
            mux.ConnectionFailed += (_, args) =>
                logger.LogWarning("Redis connection failed: {FailureType} {Exception}", args.FailureType, args.Exception?.Message);
            mux.ConnectionRestored += (_, _) =>
                logger.LogInformation("Redis connection restored");

            return mux;
        });

        services.AddSingleton<ILiveLocationCache, RedisLiveLocationCache>();
        services.AddHealthChecks()
            .AddRedis(
                sp => sp.GetRequiredService<IConnectionMultiplexer>(),
                name: "redis");
    }

    private static void ConfigureKafka(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaSettings>(options =>
        {
            configuration.GetSection(KafkaSettings.SectionName).Bind(options);

            options.BootstrapServers =
                configuration["KAFKA_BOOTSTRAP_SERVERS"]
                ?? configuration["KAFKA_BROKERS"]
                ?? options.BootstrapServers;

            if (bool.TryParse(configuration["KAFKA_ENABLED"], out var enabled))
            {
                options.Enabled = enabled;
            }

            if (bool.TryParse(configuration["KAFKA_ENABLE_CONSUMER"], out var enableConsumer))
            {
                options.EnableConsumer = enableConsumer;
            }

            options.LiveLocationTopic =
                configuration["KAFKA_LIVE_LOCATION_TOPIC"] ?? options.LiveLocationTopic;
            options.TripEventsTopic =
                configuration["KAFKA_TRIP_EVENTS_TOPIC"] ?? options.TripEventsTopic;
            options.GroupId =
                configuration["KAFKA_GROUP_ID"] ?? options.GroupId;
            options.ClientId =
                configuration["KAFKA_CLIENT_ID"] ?? options.ClientId;
        });

        var kafkaEnabled = configuration.GetValue("KAFKA_ENABLED", configuration.GetValue("Kafka:Enabled", true));
        if (!kafkaEnabled)
        {
            services.AddSingleton<ITripEventPublisher, NullTripEventPublisher>();
            return;
        }

        services.AddSingleton<ITripEventPublisher, KafkaTripEventPublisher>();
        services.AddHostedService<KafkaInitializerHostedService>();
        services.AddHostedService<KafkaTripEventsConsumerHostedService>();
    }

    /// <summary>
    /// Accepts redis:// URLs (Tripwalaah Node style) or host:port strings.
    /// </summary>
    private static string NormalizeRedisConnection(string connection)
    {
        if (string.IsNullOrWhiteSpace(connection))
        {
            return "localhost:6379";
        }

        if (!connection.StartsWith("redis://", StringComparison.OrdinalIgnoreCase)
            && !connection.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
        {
            return connection;
        }

        var uri = new Uri(connection);
        var host = string.IsNullOrWhiteSpace(uri.Host) ? "localhost" : uri.Host;
        var port = uri.Port > 0 ? uri.Port : 6379;
        var result = $"{host}:{port}";

        if (!string.IsNullOrWhiteSpace(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':', 2);
            if (parts.Length == 2)
            {
                result += $",password={parts[1]}";
            }
            else
            {
                result += $",password={parts[0]}";
            }
        }

        // redis://localhost:6379/0 → defaultDatabase=0
        if (uri.AbsolutePath is { Length: > 1 })
        {
            var dbSegment = uri.AbsolutePath.Trim('/');
            if (int.TryParse(dbSegment, out var db))
            {
                result += $",defaultDatabase={db}";
            }
        }

        if (connection.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
        {
            result += ",ssl=true";
        }

        return result;
    }
}
