using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlantCare.Infrastructure.Persistence;
using PlantCare.Infrastructure.Identity;

namespace PlantCare.Api.IntegrationTests.Infrastructure;

internal sealed class PlantCareApiFactory(bool useRealAuthentication = false,
    Action<IServiceCollection>? customizeServices = null, string? pushTestSqlPassword = null)
    : WebApplicationFactory<Program>
{
    private readonly string pushTestDatabase = "PlantCarePushTests_" + Guid.NewGuid().ToString("N");
    public static readonly DateTimeOffset UtcNow =
        new(2026, 9, 19, 10, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection databaseConnection =
        new("Data Source=:memory:");

    private readonly IServiceProvider databaseServiceProvider =
        new ServiceCollection()
            .AddEntityFrameworkSqlite()
            .AddSingleton<IModelCustomizer, SqliteTestModelCustomizer>()
            .BuildServiceProvider();

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        if (pushTestSqlPassword is null) databaseConnection.Open();

        builder.ConfigureServices(services =>
        {
            // AddDbContext stores provider setup separately from the options instance.
            // Remove SQL Server's callback before registering the test provider.
            services.RemoveAll<
                IDbContextOptionsConfiguration<PlantCareDbContext>>();
            services.RemoveAll<
                DbContextOptions<PlantCareDbContext>>();
            services.RemoveAll<PlantCareDbContext>();
            services.RemoveAll<TimeProvider>();

            services.AddDbContext<PlantCareDbContext>(options =>
            {
                if (pushTestSqlPassword is null)
                    options.UseSqlite(databaseConnection).UseInternalServiceProvider(databaseServiceProvider);
                else
                    options.UseSqlServer(new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
                    {
                        DataSource = "127.0.0.1,14330", InitialCatalog = pushTestDatabase,
                        UserID = "sa", Password = pushTestSqlPassword, TrustServerCertificate = true
                    }.ConnectionString);
            });

            if (useRealAuthentication)
            {
                services.AddSingleton(TimeProvider.System);
                customizeServices?.Invoke(services);
                return;
            }

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
            customizeServices?.Invoke(services);
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<PlantCareDbContext>();

        if (pushTestSqlPassword is null)
        {
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
        }
        else await dbContext.Database.MigrateAsync();
    }

    public async Task DeletePushTestDatabaseAsync()
    {
        if (pushTestSqlPassword is null) return;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlantCareDbContext>();
        var connection = db.Database.GetDbConnection();
        if (connection.DataSource != "127.0.0.1,14330" || connection.Database != pushTestDatabase)
            throw new InvalidOperationException("Refusing cleanup outside this generated Docker push-test database.");
        await db.Database.EnsureDeletedAsync();
    }

    public async Task<Guid> CreateUserAsync()
    {
        var userId = Guid.NewGuid();
        await SeedAsync(dbContext =>
        {
            dbContext.Users.Add(new ApplicationUser
            {
                Id = userId,
                UserName = $"{userId:N}@example.test",
                Email = $"{userId:N}@example.test",
                EmailConfirmed = true
            });
            return Task.CompletedTask;
        });
        return userId;
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

    public async Task RunOperatorCommandAsync(params string[] args)
    {
        await using var scope = Services.CreateAsyncScope();
        await PlantCare.Api.Operations.BootstrapCommands.ExecuteAsync(scope.ServiceProvider, args);
    }
}
