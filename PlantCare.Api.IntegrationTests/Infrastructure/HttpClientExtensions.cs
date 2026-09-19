using System.Text.Json;

namespace PlantCare.Api.IntegrationTests.Infrastructure;

internal static class HttpClientExtensions
{
    public static void AuthenticateAs(
        this HttpClient client,
        Guid userId,
        params string[] roles)
    {
        client.DefaultRequestHeaders.Remove(
            TestAuthenticationHandler.UserIdHeader);
        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.UserIdHeader,
            userId.ToString());

        client.DefaultRequestHeaders.Remove(
            TestAuthenticationHandler.RolesHeader);

        if (roles.Length > 0)
        {
            client.DefaultRequestHeaders.Add(
                TestAuthenticationHandler.RolesHeader,
                string.Join(',', roles));
        }
    }

    public static async Task AddAntiforgeryTokenAsync(
        this HttpClient client)
    {
        using var response = await client.GetAsync(
            "/api/antiforgery/token");
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        var requestToken = document.RootElement
            .GetProperty("requestToken")
            .GetString();

        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add(
            "X-XSRF-TOKEN",
            requestToken);
    }
}
