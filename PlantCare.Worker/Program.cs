using PlantCare.Worker;
using PlantCare.Application;
using PlantCare.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<NotificationWorkerOptions>(builder.Configuration.GetSection("Notifications"));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
