using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlantCare.Application.Abstractions.Persistence;
using PlantCare.Application.Notifications;
using PlantCare.Infrastructure.Identity;
using PlantCare.Infrastructure.Persistence;
using PlantCare.Infrastructure.Persistence.Repositories;
using PlantCare.Infrastructure.Notifications;

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

        services.AddIdentityApiEndpoints<ApplicationUser>(options =>
           {
               options.User.RequireUniqueEmail = true;

               options.Password.RequiredLength = 10;
               options.Password.RequireDigit = true;
               options.Password.RequireLowercase = true;
               options.Password.RequireUppercase = true;
               options.Password.RequireNonAlphanumeric = false;

               // TODO: add email confirmation after implementingan email provider.
               options.SignIn.RequireConfirmedEmail = false;
           })
           .AddRoles<IdentityRole<Guid>>()
           .AddEntityFrameworkStores<PlantCareDbContext>();

        services.AddScoped<IPlantSpeciesRepository, PlantSpeciesRepository>();

        services.AddScoped<IUserPlantRepository, UserPlantRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ICareScheduleRepository, CareScheduleRepository>();

        services.AddScoped<ICareEventRepository, CareEventRepository>();

        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddScoped<ICalendarRepository, CalendarRepository>();
        services.AddScoped<ICalendarSubscriptionRepository, CalendarSubscriptionRepository>();
        services.AddScoped<ICalendarShareRepository, CalendarShareRepository>();
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();
        services.AddScoped<IPushDeliveryService, WebPushDeliveryService>();
        services.Configure<WebPushOptions>(configuration.GetSection("WebPush"));

        return services;
    }
}
