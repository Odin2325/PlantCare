using Microsoft.Extensions.DependencyInjection;
using PlantCare.Application.Care;
using PlantCare.Application.Dashboard;
using PlantCare.Application.MyPlants;
using PlantCare.Application.PlantCatalog;

namespace PlantCare.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPlantSpeciesService, PlantSpeciesService>();

        services.AddScoped<IUserPlantService, UserPlantService>();

        services.AddScoped<ICareService, CareService>();

        services.AddScoped<IDashboardService, DashboardService>();

        services.AddSingleton<TimeProvider>(TimeProvider.System);

        return services;
    }
}
