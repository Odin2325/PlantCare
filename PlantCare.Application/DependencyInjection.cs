using Microsoft.Extensions.DependencyInjection;
using PlantCare.Application.PlantCatalog;

namespace PlantCare.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<
            IPlantSpeciesService,
            PlantSpeciesService>();

        return services;
    }
}