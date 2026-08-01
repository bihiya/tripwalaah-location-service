using Microsoft.Extensions.DependencyInjection;
using Tripwalaah.LocationService.Application.Interfaces;
using Tripwalaah.LocationService.Application.Services;

namespace Tripwalaah.LocationService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ILocationService, Services.LocationAppService>();
        return services;
    }
}
