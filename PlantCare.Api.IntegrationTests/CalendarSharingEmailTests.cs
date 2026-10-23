using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlantCare.Api.IntegrationTests.Infrastructure;
using PlantCare.Application.Calendar;

namespace PlantCare.Api.IntegrationTests;

public sealed class CalendarSharingEmailTests : IAsyncLifetime
{
    private readonly RecordingEmail email = new();
    private readonly PlantCareApiFactory factory;
    public CalendarSharingEmailTests() => factory = new(customizeServices: services =>
        services.Replace(ServiceDescriptor.Singleton<ICalendarShareEmailSender>(email)),
        pushTestSqlPassword: Environment.GetEnvironmentVariable("PLANTCARE_PUSH_TEST_SQL_PASSWORD"));
    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public async Task DisposeAsync() { try { await factory.DeletePushTestDatabaseAsync(); } finally { await factory.DisposeAsync(); } }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShareReportsMailOutcomeAndOwnerCanRetryWithoutDuplicatingAccess(bool accepted)
    {
        var owner = await UserAsync("owner@example.test");
        var recipient = await UserAsync("garden.friend+plants@gmail.com");
        email.Accepted = accepted;
        using var client = await Client(owner);
        var result = await client.PostAsJsonAsync("/api/calendar/shares", new { recipientEmail = " GARDEN.FRIEND+PLANTS@GMAIL.COM " });
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        var share = (await result.Content.ReadFromJsonAsync<CalendarShareDto>())!;
        Assert.Equal(accepted, share.EmailNotificationSent);
        Assert.Equal(("owner@example.test", "garden.friend+plants@gmail.com", share.Id), Assert.Single(email.Calls));
        using var received = await Client(recipient);
        Assert.Single((await received.GetFromJsonAsync<CalendarShareDto[]>("/api/calendar/shares"))!);
        email.Accepted = true;
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync($"/api/calendar/shares/{share.Id}/email", new { })).StatusCode);
        Assert.Equal(2, email.Calls.Count);
        Assert.Single((await client.GetFromJsonAsync<CalendarShareDto[]>("/api/calendar/shares"))!);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/calendar/shares", new { recipientEmail = share.RecipientEmail })).StatusCode);
        Assert.Equal(2, email.Calls.Count);
    }

    [Fact]
    public async Task ResendIsOwnerOnlyAndRevokedSharesNeverSend()
    {
        var owner = await UserAsync("owner@example.test");
        var recipient = await UserAsync("recipient@gmail.com");
        var observer = await UserAsync("observer@example.test");
        using var client = await Client(owner);
        var share = (await (await client.PostAsJsonAsync("/api/calendar/shares", new { recipientEmail = "recipient@gmail.com" }))
            .Content.ReadFromJsonAsync<CalendarShareDto>())!;
        foreach (var id in new[] { recipient, observer })
        {
            using var other = await Client(id);
            Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"/api/calendar/shares/{share.Id}/email", new { })).StatusCode);
        }
        email.Accepted = false;
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsJsonAsync($"/api/calendar/shares/{share.Id}/email", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/calendar/shares/{share.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/calendar/shares/{share.Id}/email", new { })).StatusCode);
        Assert.Equal(2, email.Calls.Count);
    }

    [Theory]
    [InlineData("not-an-address")]
    [InlineData("owner@example.test")]
    public async Task InvalidMissingOrSelfRecipientDoesNotSend(string address)
    {
        using var client = await Client(await UserAsync("owner@example.test"));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/calendar/shares", new { recipientEmail = address })).StatusCode);
        Assert.Empty(email.Calls);
    }

    private async Task<Guid> UserAsync(string address)
    {
        var id = await factory.CreateUserAsync();
        await factory.SeedAsync(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Id == id);
            user.Email = address; user.NormalizedEmail = address.ToUpperInvariant();
        });
        return id;
    }

    [Fact]
    public async Task AStaleInvitationCannotRegrantAccessAfterDirectSharingAndRevocation()
    {
        using var owner = await Client(await UserAsync("owner@example.test"));
        (await owner.PostAsJsonAsync("/api/calendar/shares", new { recipientEmail = "waiting@gmail.com" })).EnsureSuccessStatusCode();
        var token = email.Invitations.Single().Token;
        using var recipient = await Client(await UserAsync("waiting@gmail.com"));
        var created = await owner.PostAsJsonAsync("/api/calendar/shares", new { recipientEmail = "waiting@gmail.com" });
        var granted = (await created.Content.ReadFromJsonAsync<CalendarShareDto>())!;
        Assert.False(granted.IsPendingInvitation);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/calendar/shares/{granted.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await recipient.PostAsJsonAsync("/api/calendar/shares/accept-invitation", new { token })).StatusCode);
        Assert.Empty((await recipient.GetFromJsonAsync<CalendarShareDto[]>("/api/calendar/shares"))!);
    }

    [Fact]
    public async Task NewRecipientGetsPendingInvitationAndOnlyMatchingConfirmedAccountCanAcceptOnce()
    {
        using var owner = await Client(await UserAsync("owner@example.test"));
        var created = await owner.PostAsJsonAsync("/api/calendar/shares", new { recipientEmail = "new.friend@gmail.com" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var pending = (await created.Content.ReadFromJsonAsync<CalendarShareDto>())!;
        Assert.True(pending.IsPendingInvitation); Assert.True(pending.EmailNotificationSent);
        var token = Assert.Single(email.Invitations).Token;
        Assert.Equal(43, token.Length);
        await factory.SeedAsync(async db =>
        {
            Assert.Single(await db.Users.ToListAsync()); Assert.Empty(await db.CalendarShares.ToListAsync());
            Assert.NotEqual(token, (await db.CalendarInvitations.SingleAsync()).TokenHash);
        });
        using var wrong = await Client(await UserAsync("other@gmail.com"));
        Assert.Equal(HttpStatusCode.BadRequest, (await wrong.PostAsJsonAsync("/api/calendar/shares/accept-invitation", new { token })).StatusCode);
        var recipientId = await UserAsync("new.friend@gmail.com");
        using var recipient = await Client(recipientId);
        await factory.SeedAsync(async db => (await db.Users.SingleAsync(u => u.Id == recipientId)).EmailConfirmed = false);
        Assert.Equal(HttpStatusCode.BadRequest, (await recipient.PostAsJsonAsync("/api/calendar/shares/accept-invitation", new { token })).StatusCode);
        await factory.SeedAsync(async db => (await db.Users.SingleAsync(u => u.Id == recipientId)).EmailConfirmed = true);
        var accepted = await recipient.PostAsJsonAsync("/api/calendar/shares/accept-invitation", new { token });
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var share = (await accepted.Content.ReadFromJsonAsync<CalendarShareDto>())!;
        Assert.False(share.IsPendingInvitation); Assert.False(share.IsOwnedByCurrentUser);
        Assert.Equal(HttpStatusCode.BadRequest, (await recipient.PostAsJsonAsync("/api/calendar/shares/accept-invitation", new { token })).StatusCode);
        Assert.Single((await recipient.GetFromJsonAsync<CalendarShareDto[]>("/api/calendar/shares"))!);
        // A revoke initiated from an old pending card also revokes accepted access.
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/calendar/shares/{pending.Id}")).StatusCode);
        Assert.Empty((await recipient.GetFromJsonAsync<CalendarShareDto[]>("/api/calendar/shares"))!);
    }

    [Fact]
    public async Task ResendReplacesTokenAndRevocationOrExpiryPreventsAcceptance()
    {
        var ownerId = await UserAsync("owner@example.test"); using var owner = await Client(ownerId);
        email.Accepted = false;
        var response = await owner.PostAsJsonAsync("/api/calendar/shares", new { recipientEmail = "waiting@gmail.com" });
        var pending = (await response.Content.ReadFromJsonAsync<CalendarShareDto>())!;
        Assert.False(pending.EmailNotificationSent); Assert.True(pending.IsPendingInvitation);
        var oldToken = email.Invitations.Single().Token;
        email.Accepted = true;
        Assert.Equal(HttpStatusCode.Accepted, (await owner.PostAsJsonAsync($"/api/calendar/shares/{pending.Id}/email", new { })).StatusCode);
        var token = email.Invitations.Last().Token; Assert.NotEqual(oldToken, token);
        using var recipient = await Client(await UserAsync("waiting@gmail.com"));
        Assert.Equal(HttpStatusCode.BadRequest, (await recipient.PostAsJsonAsync("/api/calendar/shares/accept-invitation", new { token = oldToken })).StatusCode);
        await factory.SeedAsync(async db => db.Entry(await db.CalendarInvitations.SingleAsync()).Property(i => i.ExpiresAtUtc).CurrentValue = PlantCareApiFactory.UtcNow.AddSeconds(-1));
        Assert.Equal(HttpStatusCode.BadRequest, (await recipient.PostAsJsonAsync("/api/calendar/shares/accept-invitation", new { token })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/calendar/shares/{pending.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.PostAsJsonAsync($"/api/calendar/shares/{pending.Id}/email", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await recipient.PostAsJsonAsync("/api/calendar/shares/accept-invitation", new { token })).StatusCode);
    }
    private async Task<HttpClient> Client(Guid id)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, id.ToString());
        await client.AddAntiforgeryTokenAsync();
        return client;
    }
    private sealed class RecordingEmail : ICalendarShareEmailSender
    {
        public bool Accepted { get; set; } = true;
        public List<(string Recipient, string Token)> Invitations { get; } = [];
        public List<(string Owner, string Recipient, Guid Share)> Calls { get; } = [];
        public Task<bool> SendInvitationAsync(string ownerEmail, string recipientEmail, string token, CancellationToken cancellationToken = default)
        { Invitations.Add((recipientEmail, token)); return Task.FromResult(Accepted); }
        public Task<bool> SendAsync(string ownerEmail, string recipientEmail, Guid shareId, CancellationToken cancellationToken = default)
        { Calls.Add((ownerEmail, recipientEmail, shareId)); return Task.FromResult(Accepted); }
    }
}
