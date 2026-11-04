using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using RBBH.ConnectedParties.Helpers.Constants;

namespace IntegrationTests.Tests.PeriodLock;

public abstract class PeriodAtomicFixture : AuthenticatedApiFixture
{
    protected readonly Mock<IEmailService> Mail = new();
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Mail.Object); services.AddSingleton<RuntimeLocalizationCache>(); services.AddOptions<RuntimeLocalizationOptions>();
        services.AddScoped<RuntimeLocalizationCatalog>(); services.AddScoped<NotificationDispatcher>();
        services.Configure<EmailSettings>(options => options.AppBaseUrl = "https://app.example.test");
        Mail.Setup(mail => mail.SendApplicationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }
    protected async Task<Guid> Request(string reason = "Complete request reason")
    {
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync("/api/period-lock/request-unlock", new { year = 2026, month = 10, reason })).StatusCode);
        Guid id = default; await InDatabase(async db => id = (await db.UnlockRequests.OrderByDescending(row => row.CreatedAt).FirstAsync()).Id); return id;
    }
    protected async Task Templates()
    {
        await InDatabase(async db =>
        {
            if (await db.CodeListDefinitions.AnyAsync(row => row.Name == "Languages")) return;
            var languages = new CodeListDefinition { Name = "Languages", CreatedBy = "fixture" }; var packs = new CodeListDefinition { Name = "LanguagePacks", CreatedBy = "fixture" };
            db.CodeListDefinitions.AddRange(languages, packs); db.CodeLists.Add(new() { Kategorija = "Languages", DefinitionId = languages.Id, Kod = "BS", Naziv = "BS", KreiraoKorisnik = "fixture" });
            foreach (var kind in new[] { "requested", "response", "needsinfo", "rejected", "locked", "unlocked" })
            foreach (var part in new[] { "title", "body" })
            {
                var key = $"notifications.period.{kind}.{part}";
                db.CodeLists.Add(new() { DefinitionId = packs.Id, Kategorija = packs.Name, Kod = Guid.NewGuid().ToString("N"), Naziv = key, LanguageCode = "BS", ResourceKey = key,
                    ResourceLabel = part == "title" ? "Period notification" : kind is "locked" or "unlocked" ? "Actor {actor}: {month}/{year}" : "Message: {message}\nPeriod: {month}/{year}", KreiraoKorisnik = "fixture" });
            }
            await db.SaveChangesAsync();
        });
    }
}

public sealed class PeriodAtomicHttpTests : PeriodAtomicFixture
{
    [Fact]
    public async Task KnownRecipientInboxAndRenderedEmailUseCommittedParametersWithHtmlEncoding()
    {
        await Templates(); App.Configuration["Email:AdminEmail"] = "reviewer@example.test";
        var reason = "<script>alert('private')</script> complete reason"; var id = await Request(reason);
        using var reviewer = NewClient("reviewer");
        var inbox = await reviewer.GetFromJsonAsync<JsonElement>("/api/notifications"); Assert.Equal(1, inbox.GetProperty("total").GetInt32());
        Assert.Equal(0, (await Client.GetFromJsonAsync<JsonElement>("/api/notifications")).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await reviewer.PostAsJsonAsync($"/api/period-lock/unlock-requests/{id}/request-info", new { note = "Need more information" })).StatusCode);
        var html = new List<string>(); Mail.Setup(mail => mail.SendApplicationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, body, _) => html.Add(body)).Returns(Task.CompletedTask);
        await using var scope = App.Services.CreateAsyncScope(); Assert.Equal(2, await scope.ServiceProvider.GetRequiredService<NotificationDispatcher>().DeliverBatchAsync(default));
        Assert.Contains(html, body => body.Contains("&lt;script&gt;")); Assert.DoesNotContain(html, body => body.Contains("<script>"));
        Assert.Contains(html, body => body.Contains("Need more information"));
        await InDatabase(async db => { Assert.All(db.NotificationDeliveries, row => Assert.NotNull(row.DeliveredAt)); Assert.Equal("NEEDS_INFO", (await db.UnlockRequests.SingleAsync()).Status); });
    }
    [Fact]
    public async Task ConfiguredDistributionMailboxIsNeverAClaimableInboxIdentity()
    {
        await Request(); Guid notice = default;
        await InDatabase(async db => { var row = await db.Notifications.SingleAsync(); Assert.Equal("", row.RecipientSubject); notice = row.Id; });
        foreach (var subject in new[] { "creator", "reviewer", "unknown" })
        {
            using var client = NewClient(subject); Assert.Equal(0, (await client.GetFromJsonAsync<JsonElement>("/api/notifications")).GetProperty("total").GetInt32());
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/notifications/{notice}/read", null)).StatusCode);
        }
        using var blank = NewClient("creator"); blank.DefaultRequestHeaders.Add("X-Fixture-Empty-Subject", "true"); Assert.Contains((await blank.GetAsync("/api/notifications")).StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });
    }
    [Fact]
    public async Task BroadcastIncludesMoreThanTwoHundredKnownRecipientsAndRepeatedLockDoesNotDuplicateIt()
    {
        await InDatabase(async db =>
        {
            for (var index = 0; index < 205; index++) db.AppUsers.Add(new() { KeycloakId = "receiver-" + index, KeycloakSubjectConfirmed = true, Username = "receiver-" + index, Email = "receiver-" + index + "@example.test" });
            await db.SaveChangesAsync();
        });
        var target = new { year = DateTime.UtcNow.Year, month = DateTime.UtcNow.Month };
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync("/api/period-lock/lock", target)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PostAsJsonAsync("/api/period-lock/lock", target)).StatusCode);
        await InDatabase(async db => { Assert.Equal(208, await db.Notifications.CountAsync()); Assert.Equal(208, await db.NotificationDeliveries.CountAsync()); Assert.Single(db.AuditLogs); });
    }
    [Fact]
    public async Task MailFailureLeavesCommittedWorkflowAndRetryableDeliveryWithoutRepeatingRequests()
    {
        await Templates(); await Request();
        Mail.Setup(mail => mail.SendApplicationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("synthetic mail outage"));
        await using (var scope = App.Services.CreateAsyncScope()) Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<NotificationDispatcher>().DeliverBatchAsync(default));
        await InDatabase(async db =>
        {
            Assert.Single(db.UnlockRequests); Assert.Single(db.AuditLogs);
            var delivery = await db.NotificationDeliveries.SingleAsync(); Assert.Null(delivery.DeliveredAt); Assert.Equal(1, delivery.Attempts); delivery.NextAttemptAt = DateTime.UtcNow.AddSeconds(-1); await db.SaveChangesAsync();
        });
        Mail.Setup(mail => mail.SendApplicationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await using (var scope = App.Services.CreateAsyncScope()) Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<NotificationDispatcher>().DeliverBatchAsync(default));
        await InDatabase(async db => { Assert.Single(db.UnlockRequests); Assert.Single(db.AuditLogs); Assert.NotNull((await db.NotificationDeliveries.SingleAsync()).DeliveredAt); });
    }

    [Fact]
    public async Task CapitalRoleCanProcessRequestsWithoutAdminRole()
    {
        var id = await Request();
        await InDatabase(async db =>
        {
            var user = new AppUser { KeycloakId = "capital-reviewer", KeycloakSubjectConfirmed = true, Username = "capital-reviewer", Email = "capital-reviewer@example.test" };
            db.AppUsers.Add(user); db.UserRoles.Add(new() { UserId = user.Id, RoleId = (await db.Roles.SingleAsync(role => role.Name == ApplicationAccessRoles.Capital)).Id }); await db.SaveChangesAsync();
        });
        using var capital = NewClient("capital-reviewer"); Assert.Equal(HttpStatusCode.OK, (await capital.PostAsJsonAsync($"/api/period-lock/unlock-requests/{id}/reject", new { note = "Authorized capital decision" })).StatusCode);
        await InDatabase(async db => Assert.Equal("REJECTED", (await db.UnlockRequests.SingleAsync()).Status));
    }
}
