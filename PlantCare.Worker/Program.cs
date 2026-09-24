using PlantCare.Worker;
using PlantCare.Application;
using PlantCare.Infrastructure;
using Microsoft.AspNetCore.DataProtection;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<NotificationWorkerOptions>(builder.Configuration.GetSection("Notifications"));
builder.Services.Configure<ExternalCalendarSyncOptions>(
    builder.Configuration.GetSection("ExternalCalendarSync"));
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<ExternalCalendarSyncWorker>();

var dataProtectionKeysPath =
    builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("PlantCare")
        .PersistKeysToFileSystem(
            new DirectoryInfo(dataProtectionKeysPath));
}

var host = builder.Build();
host.Run();
