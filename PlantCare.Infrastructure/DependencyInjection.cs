using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlantCare.Infrastructure.Persistence;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Infrastructure.Persistence.Repositories;

namespace PlantCare.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PlantCareDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The connection string 'PlantCareDatabase' was not found.");
        }

        services.AddDbContext<PlantCareDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<
    IPlantSpeciesRepository,
    PlantSpeciesRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}