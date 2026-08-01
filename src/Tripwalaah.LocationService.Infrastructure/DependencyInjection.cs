using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Infrastructure.Persistence;

namespace Tripwalaah.LocationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "PostgreSQL";
        var connectionString = configuration.GetConnectionString("LocationDb");

        services.AddDbContext<LocationDbContext>(options =>
        {
            if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                options.UseInMemoryDatabase(connectionString ?? "TripwalaahLocations");
            }
            else
            {
                options.UseNpgsql(
                    connectionString
                    ?? "Host=localhost;Port=5432;Database=tripwalaah_locations;Username=postgres;Password=postgres");
            }
        });

        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddHostedService<LocationDbInitializer>();
        services.AddHealthChecks().AddDbContextCheck<LocationDbContext>("database");

        return services;
    }
}
