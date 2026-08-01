using System.Text.Json.Serialization;
using DotNetEnv;
using Tripwalaah.LocationService.Api.Hubs;
using Tripwalaah.LocationService.Api.Realtime;
using Tripwalaah.LocationService.Application;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Infrastructure;

// Load Tripwalaah-style .env if present (never commit real secrets).
if (File.Exists(".env"))
{
    Env.Load();
}
else if (File.Exists(Path.Combine("src", "Tripwalaah.LocationService.Api", ".env")))
{
    Env.Load(Path.Combine("src", "Tripwalaah.LocationService.Api", ".env"));
}

var builder = WebApplication.CreateBuilder(args);

// Map common Tripwalaah Node env vars into ASP.NET configuration.
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["MONGODB_URI"] = Environment.GetEnvironmentVariable("MONGODB_URI"),
    ["DB_MAX_POOL_SIZE"] = Environment.GetEnvironmentVariable("DB_MAX_POOL_SIZE"),
    ["DB_MIN_POOL_SIZE"] = Environment.GetEnvironmentVariable("DB_MIN_POOL_SIZE"),
    ["DB_CONNECT_TIMEOUT"] = Environment.GetEnvironmentVariable("DB_CONNECT_TIMEOUT"),
    ["DB_SOCKET_TIMEOUT"] = Environment.GetEnvironmentVariable("DB_SOCKET_TIMEOUT"),
    ["PORT"] = Environment.GetEnvironmentVariable("PORT"),
    ["API_PREFIX"] = Environment.GetEnvironmentVariable("API_PREFIX"),
    ["CORS_ALLOWED_ORIGINS"] = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS"),
    ["FRONTEND_URL"] = Environment.GetEnvironmentVariable("FRONTEND_URL"),
    ["SITE_URL"] = Environment.GetEnvironmentVariable("SITE_URL"),
    ["APP_NAME"] = Environment.GetEnvironmentVariable("APP_NAME"),
    ["SIGNALR_ENABLED"] = Environment.GetEnvironmentVariable("SIGNALR_ENABLED"),
    ["REDIS_URL"] = Environment.GetEnvironmentVariable("REDIS_URL"),
    ["REDIS_ENABLED"] = Environment.GetEnvironmentVariable("REDIS_ENABLED"),
    ["REDIS_LIVE_LOCATION_TTL_SECONDS"] = Environment.GetEnvironmentVariable("REDIS_LIVE_LOCATION_TTL_SECONDS"),
    ["KAFKA_ENABLED"] = Environment.GetEnvironmentVariable("KAFKA_ENABLED"),
    ["KAFKA_BOOTSTRAP_SERVERS"] = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS"),
    ["KAFKA_BROKERS"] = Environment.GetEnvironmentVariable("KAFKA_BROKERS"),
    ["KAFKA_ENABLE_CONSUMER"] = Environment.GetEnvironmentVariable("KAFKA_ENABLE_CONSUMER"),
    ["KAFKA_LIVE_LOCATION_TOPIC"] = Environment.GetEnvironmentVariable("KAFKA_LIVE_LOCATION_TOPIC"),
    ["KAFKA_TRIP_EVENTS_TOPIC"] = Environment.GetEnvironmentVariable("KAFKA_TRIP_EVENTS_TOPIC"),
    ["KAFKA_GROUP_ID"] = Environment.GetEnvironmentVariable("KAFKA_GROUP_ID"),
    ["KAFKA_CLIENT_ID"] = Environment.GetEnvironmentVariable("KAFKA_CLIENT_ID")
});

// Keep PORT=5000 aligned with Tripwalaah Node API; skip when host URLs are already set (e.g. tests).
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    var port = builder.Configuration["PORT"] ?? "5000";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ITripLiveUpdateService, TripLiveUpdateService>();
builder.Services.AddOpenApi();

var allowedOrigins = (builder.Configuration["CORS_ALLOWED_ORIGINS"]
        ?? "http://localhost:5173,http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .ToList();

foreach (var extra in new[] { builder.Configuration["FRONTEND_URL"], builder.Configuration["SITE_URL"] })
{
    if (!string.IsNullOrWhiteSpace(extra))
    {
        allowedOrigins.Add(extra.Trim().TrimEnd('/'));
    }
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins.Distinct(StringComparer.OrdinalIgnoreCase).ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.MapControllers();
app.MapHub<TripHub>("/hubs/trip");

app.MapGet("/", () => Results.Ok(new
{
    service = builder.Configuration["APP_NAME"] ?? "Tripwalaah.LocationService",
    version = "1.0.0",
    status = "running",
    apiPrefix = builder.Configuration["API_PREFIX"] ?? "/api",
    signalRHub = "/hubs/trip",
    redis = builder.Configuration["REDIS_URL"] ?? builder.Configuration["Redis:ConnectionString"],
    kafka = builder.Configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? builder.Configuration["Kafka:BootstrapServers"]
}));

app.Run();

public partial class Program;
