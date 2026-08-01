using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tripwalaah.LocationService.Domain.Entities;

namespace Tripwalaah.LocationService.Infrastructure.Persistence;

public sealed class LocationDbInitializer(
    IServiceProvider serviceProvider,
    IHostEnvironment environment,
    ILogger<LocationDbInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LocationDbContext>();

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (await dbContext.Locations.AnyAsync(cancellationToken))
        {
            return;
        }

        logger.LogInformation("Seeding sample locations for {Environment}", environment.EnvironmentName);

        var seed = new[]
        {
            Location.Create("Indira Gandhi International Airport", "New Delhi", "India", "IN",
                28.5562, 77.1000, LocationType.Airport, "Delhi NCR", "Primary international gateway for Delhi.", "Asia/Kolkata"),
            Location.Create("Jaipur", "Jaipur", "India", "IN",
                26.9124, 75.7873, LocationType.City, "Rajasthan", "Pink City and heritage destination.", "Asia/Kolkata"),
            Location.Create("Gateway of India", "Mumbai", "India", "IN",
                18.921984, 72.834654, LocationType.Landmark, "Maharashtra", "Iconic waterfront monument.", "Asia/Kolkata"),
            Location.Create("Changi Airport", "Singapore", "Singapore", "SG",
                1.3644, 103.9915, LocationType.Airport, null, "Major Southeast Asia hub.", "Asia/Singapore"),
            Location.Create("Dubai", "Dubai", "United Arab Emirates", "AE",
                25.2048, 55.2708, LocationType.City, "Dubai", "Global travel and leisure hub.", "Asia/Dubai")
        };

        await dbContext.Locations.AddRangeAsync(seed, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
