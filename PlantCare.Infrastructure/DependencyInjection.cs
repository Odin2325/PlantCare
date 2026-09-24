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
using PlantCare.Infrastructure.Calendar;
using PlantCare.Application.Calendar;

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

        var requireConfirmedEmail = configuration.GetValue<bool>(
            "Authentication:RequireConfirmedEmail");

        services.AddIdentityApiEndpoints<ApplicationUser>(options =>
           {
               options.User.RequireUniqueEmail = true;

               options.Password.RequiredLength = 10;
               options.Password.RequireDigit = true;
               options.Password.RequireLowercase = true;
               options.Password.RequireUppercase = true;
               options.Password.RequireNonAlphanumeric = false;

               options.SignIn.RequireConfirmedEmail =
                   requireConfirmedEmail;
           })
           .AddRoles<IdentityRole<Guid>>()
           .AddEntityFrameworkStores<PlantCareDbContext>();

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .Validate(
                options =>
                    !requireConfirmedEmail || options.IsComplete,
                "Email settings must be configured when confirmed email is required.")
            .ValidateOnStart();
        services.AddTransient<
            IEmailSender<ApplicationUser>,
            SmtpIdentityEmailSender>();

        services.AddScoped<IPlantSpeciesRepository, PlantSpeciesRepository>();

        services.AddScoped<IUserPlantRepository, UserPlantRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ICareScheduleRepository, CareScheduleRepository>();

        services.AddScoped<ICareEventRepository, CareEventRepository>();

        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddScoped<ICalendarRepository, CalendarRepository>();
        services.AddScoped<ICalendarSubscriptionRepository, CalendarSubscriptionRepository>();
        services.AddScoped<ICalendarShareRepository, CalendarShareRepository>();
        services.AddScoped<IExternalCalendarConnectionRepository, ExternalCalendarConnectionRepository>();
        services.AddHttpClient<IExternalCalendarService, GoogleCalendarService>();
        services.AddOptions<GoogleCalendarOptions>()
            .Bind(configuration.GetSection(GoogleCalendarOptions.SectionName))
            .Validate(
                options => !options.Enabled || options.IsComplete,
                "Google Calendar OAuth settings are incomplete.")
            .ValidateOnStart();
        services.AddHttpClient<IMicrosoftCalendarService, MicrosoftCalendarService>();
        services.AddOptions<MicrosoftCalendarOptions>()
            .Bind(configuration.GetSection(MicrosoftCalendarOptions.SectionName))
            .Validate(
                options => !options.Enabled || options.IsComplete,
                "Microsoft Calendar OAuth settings are incomplete.")
            .ValidateOnStart();
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();
        services.AddScoped<IPushDeliveryService, WebPushDeliveryService>();
        services.Configure<WebPushOptions>(configuration.GetSection("WebPush"));

        return services;
    }
}
