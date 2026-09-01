using Microsoft.Extensions.DependencyInjection;
using PlantCare.Application.Care;
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

        services.AddSingleton<TimeProvider>(TimeProvider.System);

        return services;
    }
}