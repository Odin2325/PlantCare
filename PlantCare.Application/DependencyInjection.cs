using Microsoft.Extensions.DependencyInjection;
using PlantCare.Application.PlantCatalog;
using PlantCare.Application.MyPlants;

namespace PlantCare.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IPlantSpeciesService, PlantSpeciesService>();

        services.AddScoped<IUserPlantService, UserPlantService>();

        return services;
    }
}