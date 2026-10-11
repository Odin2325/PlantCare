using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PlantCare.Api.IntegrationTests.Infrastructure;
using PlantCare.Infrastructure.Identity;

namespace PlantCare.Api.IntegrationTests;

public sealed class AuthenticationSecurityTests : IAsyncLifetime
{
    private readonly PlantCareApiFactory factory = new(useRealAuthentication: true);
    private const string OriginalPassword = "OriginalPassword9!";
    private const string NewPassword = "ReplacementPassword9!";
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public async Task DisposeAsync() => await factory.DisposeAsync();
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
    });

    private async Task<(string Email, string ResetCode)> CreateUserAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"security-{Guid.NewGuid():N}@example.test";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        Assert.True((await manager.CreateAsync(user, OriginalPassword)).Succeeded);
        var token = await manager.GeneratePasswordResetTokenAsync(user);
        return (email, WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token)));
    }

    private static async Task LoginAsync(HttpClient client, string email, string password)
    {
        await client.AddAntiforgeryTokenAsync();
        using var response = await client.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email, password });
        response.EnsureSuccessStatusCode();
        await client.AddAntiforgeryTokenAsync();
    }

    [Fact]
    public async Task BogusBearerHeaderDoesNotBypassAnonymousAntiforgery()
    {
        using var client = Client();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        using var response = await client.PostAsJsonAsync("/api/auth/forgotPassword", new { email = "unknown@example.test" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RealBearerRequestsWorkWithoutAntiforgery()
    {
        var (email, _) = await CreateUserAsync();
        using var client = Client();
        await client.AddAntiforgeryTokenAsync();
        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = OriginalPassword });
        login.EnsureSuccessStatusCode();
        using var tokens = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.RootElement.GetProperty("accessToken").GetString());
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        using var response = await client.PutAsJsonAsync("/api/notifications/preferences", new
        {
            inAppEnabled = true, pushEnabled = false, wateringEnabled = true, reminderLeadTimeHours = 0
        });
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task PasswordResetRevokesExistingCookiesAndRefreshTokenAndCannotBeReused()
    {
        var (email, resetCode) = await CreateUserAsync();
        using var browserA = Client();
        using var browserB = Client();
        using var requester = Client();
        await LoginAsync(browserA, email, OriginalPassword);
        await LoginAsync(browserB, email, OriginalPassword);
        await requester.AddAntiforgeryTokenAsync();
        using var bearerLogin = await requester.PostAsJsonAsync("/api/auth/login", new { email, password = OriginalPassword });
        bearerLogin.EnsureSuccessStatusCode();
        using var tokens = JsonDocument.Parse(await bearerLogin.Content.ReadAsStringAsync());
        var refreshToken = tokens.RootElement.GetProperty("refreshToken").GetString();

        using var reset = await requester.PostAsJsonAsync("/api/auth/resetPassword", new { email, resetCode, newPassword = NewPassword });
        reset.EnsureSuccessStatusCode();
        using var staleA = await browserA.GetAsync("/api/account/me");
        using var staleB = await browserB.GetAsync("/api/account/me");
        Assert.Equal(HttpStatusCode.Unauthorized, staleA.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, staleB.StatusCode);
        using var refresh = await requester.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        using var reused = await requester.PostAsJsonAsync("/api/auth/resetPassword", new { email, resetCode, newPassword = OriginalPassword });
        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);
        using var oldLogin = await requester.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email, password = OriginalPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        await LoginAsync(requester, email, NewPassword);
    }

    [Fact]
    public async Task ResetCodeCannotBeUsedForAnotherConfirmedAccount()
    {
        var (ownerEmail, resetCode) = await CreateUserAsync();
        var (otherEmail, _) = await CreateUserAsync();
        using var client = Client();
        await client.AddAntiforgeryTokenAsync();
        using var reset = await client.PostAsJsonAsync("/api/auth/resetPassword", new { email = otherEmail, resetCode, newPassword = NewPassword });
        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
        await LoginAsync(client, otherEmail, OriginalPassword);
        await using var scope = factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True(await manager.CheckPasswordAsync((await manager.FindByEmailAsync(ownerEmail))!, OriginalPassword));
    }

    [Fact]
    public async Task ExpiredResetCodeCannotChangePassword()
    {
        var (email, resetCode) = await CreateUserAsync();
        // Expire an already-issued token via test-only provider options. The real
        // provider uses wall-clock UTC, so no host clock or token format is changed.
        factory.Services.GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>().Value.TokenLifespan = TimeSpan.Zero;
        using var client = Client();
        await client.AddAntiforgeryTokenAsync();
        using var reset = await client.PostAsJsonAsync("/api/auth/resetPassword", new { email, resetCode, newPassword = NewPassword });
        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
        await LoginAsync(client, email, OriginalPassword);
    }

    [Fact]
    public async Task ExpiredAuthenticationCookieIsRejected()
    {
        var (email, _) = await CreateUserAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await manager.FindByEmailAsync(email))!;
        var principal = await scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>().CreateUserPrincipalAsync(user);
        var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentityConstants.ApplicationScheme);
        var ticket = new AuthenticationTicket(principal, new AuthenticationProperties
        {
            IssuedUtc = DateTimeOffset.UtcNow.AddDays(-8), ExpiresUtc = DateTimeOffset.UtcNow.AddDays(-1)
        }, IdentityConstants.ApplicationScheme);
        var cookie = options.TicketDataFormat.Protect(ticket);
        using var client = Client();
        client.DefaultRequestHeaders.Add("Cookie", $"{options.Cookie.Name}={cookie}");
        using var response = await client.GetAsync("/api/account/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
