using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlantCare.Infrastructure.Persistence;

namespace PlantCare.Api.IntegrationTests.Infrastructure;

internal sealed class PlantCareApiFactory
    : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset UtcNow =
        new(2026, 9, 19, 10, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection databaseConnection =
        new("Data Source=:memory:");

    private readonly IServiceProvider databaseServiceProvider =
        new ServiceCollection()
            .AddEntityFrameworkSqlite()
            .BuildServiceProvider();

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        databaseConnection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<
                DbContextOptions<PlantCareDbContext>>();
            services.RemoveAll<PlantCareDbContext>();
            services.RemoveAll<TimeProvider>();

            services.AddDbContext<PlantCareDbContext>(options =>
                options
                    .UseSqlite(databaseConnection)
                    .UseInternalServiceProvider(
                        databaseServiceProvider));

            services.AddSingleton<TimeProvider>(
                new FixedTimeProvider(UtcNow));

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme =
                        TestAuthenticationHandler.SchemeName;
                    options.DefaultForbidScheme =
                        TestAuthenticationHandler.SchemeName;
                })
                .AddScheme<
                    AuthenticationSchemeOptions,
                    TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    _ => { });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<PlantCareDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task SeedAsync(
        Func<PlantCareDbContext, Task> seed)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<PlantCareDbContext>();

        await seed(dbContext);
        await dbContext.SaveChangesAsync();
    }
}
