using Microsoft.Extensions.DependencyInjection;
using PlantCare.Application.Care;
using PlantCare.Application.Calendar;
using PlantCare.Application.Dashboard;
using PlantCare.Application.MyPlants;
using PlantCare.Application.Notifications;
using PlantCare.Application.PlantCatalog;

namespace PlantCare.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPlantSpeciesService, PlantSpeciesService>();

        services.AddScoped<IUserPlantService, UserPlantService>();

        services.AddScoped<ICareService, CareService>();

        services.AddScoped<ICalendarService, CalendarService>();

        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddSingleton<TimeProvider>(TimeProvider.System);

        return services;
    }
}
