using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using PlantCare.Api.Errors;
using PlantCare.Api.Health;
using PlantCare.Api.Security;
using PlantCare.Application;
using PlantCare.Infrastructure;
using PlantCare.Infrastructure.Identity;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlantCare.Infrastructure.Persistence;

const string AngularDevelopmentCorsPolicy = "AngularDevelopment";
var builder = WebApplication.CreateBuilder(args);

// Add ASP.NET Core services.
builder.Services
    .AddControllersWithViews(options =>
    {
        options.Filters.Add<ConditionalAntiforgeryFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// Add PlantCare application layers.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "PlantCare.Authentication";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;

    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.None : CookieSecurePolicy.Always;

    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        AngularDevelopmentCorsPolicy,
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:62018",
                    "https://localhost:62018")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddAuthorization();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    if (builder.Configuration.GetValue<bool>(
        "ReverseProxy:TrustForwardedHeaders"))
    {
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.ForwardLimit = 1;
    }
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.AddPolicy("public-feed", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests.",
            Detail = "Please wait before trying again."
        }, cancellationToken);
    };
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";

    options.Cookie.Name = "PlantCare.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;

    options.Cookie.SecurePolicy =
        builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.None
            : CookieSecurePolicy.Always;
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();

    if (app.Configuration.GetValue<bool>(
        "Database:ApplyMigrationsOnStartup"))
    {
        var database = scope.ServiceProvider
            .GetRequiredService<PlantCareDbContext>();
        await database.Database.MigrateAsync();
    }

    await RoleSeeder.SeedAsync(scope.ServiceProvider);

    if (app.Environment.IsDevelopment())
    {
        var developmentAdminEmail =
            app.Configuration["DevelopmentAdmin:Email"];

        if (!string.IsNullOrWhiteSpace(developmentAdminEmail))
        {
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var user = await userManager.FindByEmailAsync(
                developmentAdminEmail);

            if (user is null)
            {
                app.Logger.LogWarning(
                    "Development admin user {Email} was not found. Create the account and restart the API to assign the Admin role.",
                    developmentAdminEmail);
            }
            else if (!await userManager.IsInRoleAsync(user, "Admin"))
            {
                var result = await userManager.AddToRoleAsync(
                    user,
                    "Admin");

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        result.Errors.Select(error => error.Description));

                    app.Logger.LogError(
                        "Could not assign the Admin role to development user {Email}: {Errors}",
                        developmentAdminEmail,
                        errors);
                }
            }
        }
    }
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseCors(AngularDevelopmentCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet(
        "/api/antiforgery/token",
        (
            HttpContext httpContext,
            IAntiforgery antiforgery) =>
        {
            var tokenSet =
                antiforgery.GetAndStoreTokens(httpContext);

            if (string.IsNullOrWhiteSpace(
                tokenSet.RequestToken))
            {
                return Results.Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title:
                        "Unable to create an antiforgery token.");
            }

            return Results.Ok(new
            {
                requestToken = tokenSet.RequestToken
            });
        })
    .AllowAnonymous();

var authenticationGroup = app.MapGroup("/api/auth");
authenticationGroup.RequireRateLimiting("authentication");

authenticationGroup.MapIdentityApi<ApplicationUser>();

authenticationGroup.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.NoContent();
        })
    .RequireAuthorization();

authenticationGroup.RequireAntiforgeryValidation();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapHealthChecks("/health");
app.MapMethods(
    "/api/{**path}",
    ["GET", "HEAD", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"],
    () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
