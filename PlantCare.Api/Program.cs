using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PlantCare.Api.Security;
using PlantCare.Application;
using PlantCare.Infrastructure;
using PlantCare.Infrastructure.Identity;
using System.Text.Json.Serialization;

const string AngularDevelopmentCorsPolicy = "AngularDevelopment";
var builder = WebApplication.CreateBuilder(args);

// Add ASP.NET Core services.
builder.Services
    .AddControllersWithViews(options =>
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

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

using (var scope = app.Services.CreateScope())
{
    await RoleSeeder.SeedAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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

app.MapGroup("/api/auth").MapIdentityApi<ApplicationUser>().RequireAntiforgeryValidation();

app.MapPost("/api/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
    {
        await signInManager.SignOutAsync();

        return Results.NoContent();
    })
    .RequireAuthorization()
    .RequireAntiforgeryValidation();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
