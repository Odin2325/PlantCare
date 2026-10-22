using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using PlantCare.Api.IntegrationTests.Infrastructure;
using PlantCare.Application.Notifications;
using PlantCare.Domain.Entities;
using PlantCare.Domain.Enums;
using PlantCare.Domain.ValueObjects;
using PlantCare.Infrastructure.Notifications;
using PlantCare.Infrastructure.Persistence;
using WebPush;
using PushSubscription = PlantCare.Domain.Entities.PushSubscription;

namespace PlantCare.Api.IntegrationTests;

public sealed class PushDeliveryTests : IAsyncLifetime
{
    private readonly MutableClock clock = new();
    private readonly ProviderHandler provider = new();
    private readonly PlantCareApiFactory factory;
    private readonly VapidDetails keys = VapidHelper.GenerateVapidKeys();
    private readonly MutableOptions settings = new();
    private readonly List<string> payloads = [];

    public PushDeliveryTests()
    {
        settings.Value = new WebPushOptions
        { Subject = "mailto:push-tests@example.test", PublicKey = keys.PublicKey, PrivateKey = keys.PrivateKey };
        factory = new(customizeServices: services =>
        {
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));
            services.Replace(ServiceDescriptor.Singleton<IOptions<WebPushOptions>>(settings));
            var transport = services.Last(item => item.ServiceType == typeof(IWebPushTransport));
            services.Replace(ServiceDescriptor.Transient<IWebPushTransport>(provider =>
                new RecordingTransport((IWebPushTransport)transport.ImplementationFactory!(provider), payloads)));
            services.Configure<HttpClientFactoryOptions>(nameof(IWebPushTransport), options =>
                options.HttpMessageHandlerBuilderActions.Add(builder => builder.PrimaryHandler = provider));
        }, pushTestSqlPassword: Environment.GetEnvironmentVariable("PLANTCARE_PUSH_TEST_SQL_PASSWORD"));
    }

    public Task InitializeAsync() => factory.ResetDatabaseAsync();
    public async Task DisposeAsync()
    {
        try { await factory.DeletePushTestDatabaseAsync(); }
        finally { await factory.DisposeAsync(); }
    }

    [Fact]
    public async Task ReminderContainsAngularNotificationBodyAndClickDestination()
    {
        var id = await SeedAsync("payload-contract");
        Assert.Equal(1, await DeliverAsync());
        using var document = JsonDocument.Parse(Assert.Single(payloads));
        var notification = document.RootElement.GetProperty("notification");
        Assert.Equal("Watering reminder", notification.GetProperty("title").GetString());
        Assert.Equal("Push test plant needs care.", notification.GetProperty("body").GetString());
        Assert.Equal("/icons/care-watering.png", notification.GetProperty("icon").GetString());
        Assert.Equal($"plantcare-{id:N}", notification.GetProperty("tag").GetString());
        var click = notification.GetProperty("data").GetProperty("onActionClick").GetProperty("default");
        Assert.Equal("openWindow", click.GetProperty("operation").GetString());
        Assert.Equal("/dashboard", click.GetProperty("url").GetString());
        Assert.False(notification.TryGetProperty("options", out _));
        Assert.Equal(1, provider.EncryptedRequests);
    }

    [Theory]
    [InlineData(CareActionType.Watering, "/icons/care-watering.png")]
    [InlineData(CareActionType.Fertilizing, "/icons/care-fertilizing.png")]
    [InlineData(CareActionType.Misting, "/icons/care-misting.png")]
    [InlineData(CareActionType.Pruning, "/icons/care-pruning.png")]
    [InlineData(CareActionType.Repotting, "/icons/care-repotting.png")]
    [InlineData(CareActionType.Unknown, "/icons/plantcare-192.png")]
    public async Task ReminderUsesCareActionIconRegardlessOfCatalogPhoto(CareActionType action, string icon)
    {
        await SeedAsync("care-action-icon");
        await InspectAsync(async db =>
        {
            var schedule = await db.CareSchedules.Include(s => s.UserPlant).ThenInclude(p => p.PlantSpecies).SingleAsync();
            // Include an unknown legacy action to exercise the app-icon fallback.
            db.Entry(schedule).Property(s => s.ActionType).CurrentValue = action;
            schedule.UserPlant.PlantSpecies.SetCatalogProfile(new PlantCatalogProfile(
                "https://example.org/care", new(2026, 10, 22), new PlantCatalogPhoto(
                    "/images/plants/monstera-deliciosa.jpg", "Plant reference", "Test photographer",
                    "https://example.org/photo", "CC BY 4.0", "https://creativecommons.org/licenses/by/4.0/")));
            await db.SaveChangesAsync();
        });
        Assert.Equal(1, await DeliverAsync());
        using var document = JsonDocument.Parse(Assert.Single(payloads));
        var notification = document.RootElement.GetProperty("notification");
        Assert.Equal(icon, notification.GetProperty("icon").GetString());
        Assert.Equal("Push test plant needs care.", notification.GetProperty("body").GetString());
        Assert.False(notification.GetProperty("data").TryGetProperty("photo", out _));
        Assert.Equal("/dashboard", notification.GetProperty("data").GetProperty("onActionClick").GetProperty("default").GetProperty("url").GetString());
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(payloads.Single()) < 3_000);
        Assert.Equal(1, provider.EncryptedRequests);
    }

    [Fact]
    public async Task LegacyBlankNicknameFallsBackToCommonNameInReminder()
    {
        await SeedAsync("common-name-fallback");
        await InspectAsync(async db =>
        {
            var plant = await db.UserPlants.SingleAsync();
            db.Entry(plant).Property(p => p.Nickname).CurrentValue = " ";
            await db.SaveChangesAsync();
        });
        Assert.Equal(1, await DeliverAsync());
        using var document = JsonDocument.Parse(Assert.Single(payloads));
        Assert.Equal("Synthetic push plant needs care.",
            document.RootElement.GetProperty("notification").GetProperty("body").GetString());
    }

    [Fact]
    public async Task PartialFailurePersistsAcceptedDevicesAcrossFreshWorkerScopes()
    {
        var notification = await SeedAsync("good", "retry");
        provider.Respond = endpoint => endpoint == "retry" && provider.Count("retry") == 1
            ? new(HttpStatusCode.ServiceUnavailable) : new(HttpStatusCode.Created);
        Assert.Equal(0, await DeliverAsync());
        Assert.Equal(1, provider.Count("good"));
        Assert.Equal(0, await DeliverAsync()); // Backoff, even in another scope.
        Assert.Equal(1, provider.Count("retry"));
        clock.Now = clock.Now.AddMinutes(1);
        Assert.Equal(1, await DeliverAsync());
        Assert.Equal(1, provider.Count("good"));
        Assert.Equal(2, provider.Count("retry"));
        await InspectAsync(async db =>
        {
            Assert.All(await db.PushDeliveries.ToListAsync(), item => Assert.Equal(PushDeliveryStatus.Accepted, item.Status));
            Assert.NotNull((await db.Notifications.FindAsync(notification))!.PushSentAtUtc);
        });
        Assert.Equal(0, await DeliverAsync());
        Assert.All(provider.Ttls, value => Assert.Equal("3600", value));
        Assert.True(provider.EncryptedRequests > 0);
    }

    [Theory]
    [InlineData(404)]
    [InlineData(410)]
    public async Task ExpiredDeviceIsRemovedWithoutResendingHealthyDevice(int status)
    {
        await SeedAsync("gone", "good");
        provider.Respond = endpoint => new(endpoint == "gone" ? (HttpStatusCode)status : HttpStatusCode.Created);
        Assert.Equal(1, await DeliverAsync());
        Assert.Equal(0, await DeliverAsync());
        await InspectAsync(async db =>
        {
            Assert.Single(await db.PushSubscriptions.ToListAsync());
            Assert.Contains(await db.PushDeliveries.ToListAsync(), item => item.Status == PushDeliveryStatus.Expired);
        });
        Assert.Equal(1, provider.Count("good"));
    }

    [Fact]
    public async Task FailedOldestReminderDoesNotBlockAnotherUserAndRetriesStop()
    {
        var failing = await SeedAsync("bad");
        clock.Now = clock.Now.AddSeconds(1);
        await SeedAsync("healthy");
        provider.Respond = endpoint => new(endpoint == "bad" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.Created);
        Assert.Equal(0, await DeliverAsync(1));
        Assert.Equal(1, await DeliverAsync(1));
        Assert.Equal(1, provider.Count("healthy"));
        for (var retry = 0; retry < 4; retry++)
        {
            await InspectAsync(async db => clock.Now = (await db.Notifications.FindAsync(failing))!.PushNextAttemptAtUtc!.Value);
            await DeliverAsync(1);
        }
        Assert.Equal(5, provider.Count("bad"));
        Assert.Equal(0, await DeliverAsync());
        await InspectAsync(async db => Assert.Equal(PushDeliveryStatus.Failed,
            (await db.PushDeliveries.SingleAsync(item => item.NotificationId == failing)).Status));
    }

    [Theory]
    [InlineData("archived")]
    [InlineData("paused")]
    [InlineData("completed")]
    [InlineData("preference")]
    [InlineData("action")]
    [InlineData("removed-device")]
    [InlineData("too-old")]
    public async Task PendingRetryIsSuppressedWhenNoLongerRelevant(string change)
    {
        var id = await SeedAsync("retry");
        provider.Respond = _ => new(HttpStatusCode.ServiceUnavailable);
        await DeliverAsync();
        clock.Now = clock.Now.AddMinutes(1);
        await InspectAsync(async db =>
        {
            var notification = await db.Notifications.Include(item => item.CareSchedule).ThenInclude(item => item.UserPlant).SingleAsync();
            switch (change)
            {
                case "archived": notification.CareSchedule.UserPlant.Archive(); break;
                case "paused": notification.CareSchedule.Disable(); break;
                case "completed": notification.CareSchedule.MarkCompleted(clock.Now); break;
                case "removed-device": db.PushSubscriptions.RemoveRange(db.PushSubscriptions); break;
                case "too-old": clock.Now = clock.Now.AddDays(1); break;
                default:
                    var preference = NotificationPreference.CreateDefault(notification.UserId);
                    preference.Update(true, change != "preference", change != "action", true, true, true, true, 0);
                    db.NotificationPreferences.Add(preference);
                    break;
            }
            await db.SaveChangesAsync();
        });
        Assert.Equal(1, await DeliverAsync());
        Assert.Equal(1, provider.Count("retry"));
        await InspectAsync(async db => Assert.NotNull((await db.Notifications.FindAsync(id))!.PushSentAtUtc));
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(413)]
    [InlineData(302)]
    public async Task PermanentProviderErrorIsTerminalButKeepsSubscription(int status)
    {
        await SeedAsync("failed");
        provider.Respond = _ => new((HttpStatusCode)status);
        Assert.Equal(1, await DeliverAsync());
        Assert.Equal(0, await DeliverAsync());
        await InspectAsync(async db =>
        {
            Assert.Single(await db.PushSubscriptions.ToListAsync());
            Assert.Equal(PushDeliveryStatus.Failed, (await db.PushDeliveries.SingleAsync()).Status);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetryAfterIsHonouredAndNewDeviceDoesNotReplayOldReminder(bool absoluteDate)
    {
        await SeedAsync("limited");
        provider.Respond = _ => new(HttpStatusCode.TooManyRequests) { Headers = { RetryAfter = absoluteDate
            ? new RetryConditionHeaderValue(clock.Now.AddMinutes(10)) : new RetryConditionHeaderValue(TimeSpan.FromMinutes(10)) } };
        await DeliverAsync();
        clock.Now = clock.Now.AddMinutes(5);
        Assert.Equal(0, await DeliverAsync());
        await InspectAsync(async db =>
        {
            var userId = (await db.Notifications.SingleAsync()).UserId;
            db.PushSubscriptions.Add(Device(userId, "new-device"));
            await db.SaveChangesAsync();
        });
        clock.Now = clock.Now.AddMinutes(5);
        provider.Respond = _ => new(HttpStatusCode.Created);
        Assert.Equal(1, await DeliverAsync());
        Assert.Equal(0, provider.Count("new-device"));
    }

    [Fact]
    public async Task CallerCancellationPropagatesWithoutRecordingAProviderFailure()
    {
        await SeedAsync("cancel");
        using var cancellation = new CancellationTokenSource();
        provider.BeforeResponse = () => cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DeliverAsync(100, cancellation.Token));
        await InspectAsync(async db =>
        {
            var delivery = await db.PushDeliveries.SingleAsync();
            Assert.Equal(0, delivery.AttemptCount);
            Assert.Equal(PushDeliveryStatus.Pending, delivery.Status);
        });
    }

    [Fact]
    public async Task InterruptedPassKeepsTheFirstAcceptedDeviceDurable()
    {
        await SeedAsync("first-device", "second-device");
        using var cancellation = new CancellationTokenSource();
        provider.BeforeResponse = () => { if (provider.TotalCount == 2) cancellation.Cancel(); };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DeliverAsync(100, cancellation.Token));
        await InspectAsync(async db =>
        {
            var deliveries = await db.PushDeliveries.ToListAsync();
            Assert.Single(deliveries, item => item.Status == PushDeliveryStatus.Accepted);
            Assert.Single(deliveries, item => item.Status == PushDeliveryStatus.Pending && item.AttemptCount == 0);
        });
        provider.BeforeResponse = null;
        Assert.Equal(1, await DeliverAsync());
        Assert.Equal(3, provider.TotalCount);
    }

    [Fact]
    public async Task NetworkFailureBacksOffAndNoSubscriptionsFinishWithoutSending()
    {
        await SeedAsync("network");
        provider.BeforeResponse = () => throw new HttpRequestException("Synthetic network failure");
        Assert.Equal(0, await DeliverAsync());
        Assert.Equal(0, await DeliverAsync());
        await InspectAsync(async db => Assert.Equal(clock.Now.AddMinutes(1),
            (await db.PushDeliveries.SingleAsync()).NextAttemptAtUtc));
        clock.Now = clock.Now.AddSeconds(1);
        await SeedAsync();
        Assert.Equal(1, await DeliverAsync());
    }

    [Fact]
    public async Task LargeDeviceFanoutYieldsToAnotherUserWithoutReplayingAcceptedDevices()
    {
        var devices = Enumerable.Range(0, 20).Select(index => $"device-{index}").ToArray();
        await SeedAsync(devices);
        Assert.Equal(0, await DeliverAsync());
        Assert.Equal(16, devices.Sum(provider.Count));
        clock.Now = clock.Now.AddSeconds(1);
        await SeedAsync("another-user");
        Assert.Equal(1, await DeliverAsync());
        Assert.Equal(1, provider.Count("another-user"));
        clock.Now = clock.Now.AddMinutes(1);
        Assert.Equal(1, await DeliverAsync());
        Assert.All(devices, device => Assert.Equal(1, provider.Count(device)));
    }

    [Fact]
    public async Task TransportTimeoutRetriesButDoesNotBlockAnotherUser()
    {
        await SeedAsync("timeout");
        clock.Now = clock.Now.AddSeconds(1);
        await SeedAsync("healthy");
        provider.Respond = endpoint => endpoint == "timeout"
            ? throw new TaskCanceledException("Synthetic transport timeout") : new(HttpStatusCode.Created);
        Assert.Equal(1, await DeliverAsync());
        Assert.Equal(1, provider.Count("healthy"));
        Assert.Equal(0, await DeliverAsync());
        Assert.Equal(1, provider.Count("timeout"));
    }

    [Fact]
    public async Task UnconfiguredPushLeavesTheQueueUntouched()
    {
        var id = await SeedAsync("configured-later");
        settings.Value = new WebPushOptions();
        Assert.Equal(0, await DeliverAsync());
        Assert.Equal(0, provider.Count("configured-later"));
        await InspectAsync(async db =>
        {
            Assert.Empty(await db.PushDeliveries.ToListAsync());
            Assert.Null((await db.Notifications.FindAsync(id))!.PushSentAtUtc);
        });
    }

    private async Task<int> DeliverAsync(int batch = 100, CancellationToken cancellationToken = default)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IPushDeliveryService>().DeliverPendingAsync(batch, cancellationToken);
    }

    private async Task InspectAsync(Func<PlantCareDbContext, Task> inspect)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await inspect(scope.ServiceProvider.GetRequiredService<PlantCareDbContext>());
    }

    private async Task<Guid> SeedAsync(params string[] devices)
    {
        var userId = await factory.CreateUserAsync();
        var species = PlantSpecies.Create("Synthetic push plant", null, "Test data", SunlightRequirement.BrightIndirectLight,
            "Test", 1, "Test", null, null, "Test", null, null, null, false);
        var plant = UserPlant.Create(userId, species.Id, "Push test plant", null, null, null, clock.Now.AddDays(-2));
        var schedule = plant.AddCareSchedule(CareActionType.Watering, 1, clock.Now.AddDays(-2));
        var notification = Notification.Create(userId, schedule.Id, schedule.NextDueAtUtc!.Value, clock.Now);
        await factory.SeedAsync(db =>
        {
            db.PlantSpecies.Add(species); db.UserPlants.Add(plant); db.Notifications.Add(notification);
            db.PushSubscriptions.AddRange(devices.Select(name => Device(userId, name)));
            return Task.CompletedTask;
        });
        return notification.Id;
    }

    private PushSubscription Device(Guid userId, string name) => PushSubscription.Create(userId,
        $"https://push.example.test/{name}", keys.PublicKey,
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)).TrimEnd('=').Replace('+', '-').Replace('/', '_'), clock.Now);

    private sealed class RecordingTransport(IWebPushTransport inner, List<string> payloads) : IWebPushTransport
    {
        public Task<PushSendResult> SendAsync(PushSubscription subscription, string payload, CancellationToken cancellationToken)
        {
            payloads.Add(payload);
            return inner.SendAsync(subscription, payload, cancellationToken);
        }
    }

    private sealed class MutableClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = PlantCareApiFactory.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class MutableOptions : IOptions<WebPushOptions>
    {
        public WebPushOptions Value { get; set; } = new();
    }

    private sealed class ProviderHandler : HttpMessageHandler
    {
        public Func<string, HttpResponseMessage> Respond { get; set; } = _ => new(HttpStatusCode.Created);
        public Action? BeforeResponse { get; set; }
        private readonly Dictionary<string, int> calls = [];
        public List<string> Ttls { get; } = [];
        public int EncryptedRequests { get; private set; }
        public int Count(string device) => calls.GetValueOrDefault(device);
        public int TotalCount => calls.Values.Sum();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("push.example.test", request.RequestUri!.Host); // Never contact a real provider.
            var device = request.RequestUri.AbsolutePath.Trim('/');
            calls[device] = Count(device) + 1;
            Ttls.Add(request.Headers.GetValues("TTL").Single());
            if (request.Content?.Headers.ContentEncoding.Contains("aesgcm") == true) EncryptedRequests++;
            BeforeResponse?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Respond(device));
        }
    }
}
