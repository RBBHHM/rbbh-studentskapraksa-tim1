using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.Helpers.Constants;

namespace IntegrationTests.Tests.Users;

public abstract class RoleRequestFixture : AuthenticatedApiFixture
{
    protected readonly Mock<IEmailService> _mail = new();
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<RoleRequestService>(); services.AddScoped<NotificationDispatcher>();
        services.AddSingleton<RuntimeLocalizationCache>(); services.AddOptions<RuntimeLocalizationOptions>();
        services.AddScoped<RuntimeLocalizationCatalog>();
        services.Configure<EmailSettings>(options => options.AppBaseUrl = "https://app.example.test");
        _mail.Setup(mail => mail.SendApplicationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        services.AddSingleton(_mail.Object);
    }

    protected async Task<Guid> Role(string name)
    {
        Guid id = Guid.Empty;
        await InDatabase(async db =>
        {
            // InMemory has no migrations; seed the same logical prerequisites explicitly.
            if (!await db.CodeListDefinitions.AnyAsync(row => row.Name == "LanguagePacks"))
            {
                var languages = new RBBH.ConnectedParties.DL.Entities.Sifarnici.CodeListDefinition { Name = "Languages", CreatedBy = "fixture" };
                var packs = new RBBH.ConnectedParties.DL.Entities.Sifarnici.CodeListDefinition { Name = "LanguagePacks", CreatedBy = "fixture" };
                db.CodeListDefinitions.AddRange(languages, packs);
                db.CodeLists.Add(new() { Kategorija = "Languages", DefinitionId = languages.Id, Kod = "BS", Naziv = "Bosanski", KreiraoKorisnik = "fixture" });
                foreach (var outcome in new[] { "submitted", "approved", "rejected" })
                foreach (var part in new[] { "title", "body" })
                {
                    var key = $"notifications.role.request.{outcome}.{part}";
                    db.CodeLists.Add(new() { Kategorija = "LanguagePacks", DefinitionId = packs.Id, Kod = Guid.NewGuid().ToString("N"), Naziv = key,
                        ResourceKey = key, LanguageCode = "BS", ResourceLabel = "Fixture notification " + outcome, KreiraoKorisnik = "fixture" });
                }
                await db.SaveChangesAsync();
            }
            id = (await db.Roles.SingleAsync(role => role.Name == name)).Id;
        });
        return id;
    }

    protected async Task<JsonElement> Submit(HttpClient client, string purpose = "new", Guid[]? roles = null, Guid? key = null)
    {
        var response = await client.PostAsJsonAsync("/api/role-requests", new
        {
            clientRequestId = key ?? Guid.NewGuid(), purpose, roleIds = roles ?? new[] { await Role(ApplicationAccessRoles.LegalPersons) },
            note = "Fixture role request", requesterSubject = "forged", requesterEmail = "forged@example.test"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

}

public sealed class RoleRequestWorkflowHttpTests : RoleRequestFixture
{
    [Fact]
    public async Task ChangeRequestCanRemoveExplicitRolesWithoutRemovingGuestBaseline()
    {
        using var applicant = NewClient("admin2"); var row = await Submit(applicant, purpose: "change", roles: []);
        var id = row.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync($"/api/role-requests/{id}/approve", new { decisionId = Guid.NewGuid(), expectedVersion = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await applicant.GetAsync("/api/role-requests")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await applicant.GetAsync("/api/role-requests/options")).StatusCode);
        await InDatabase(async db =>
        {
            var user = await db.AppUsers.SingleAsync(user => user.KeycloakId == "admin2");
            Assert.False(await db.UserRoles.AnyAsync(item => item.UserId == user.Id && item.IsActive));
            Assert.True(user.IsActive); Assert.Equal(2, await db.UserRoles.CountAsync(item => item.IsActive));
        });
    }

    [Fact]
    public async Task UnverifiedEmailAndUnknownRoleCannotCreateARequest()
    {
        using var guest = NewClient("guest"); guest.DefaultRequestHeaders.Add("X-Email-Verified", "false");
        var body = new { clientRequestId = Guid.NewGuid(), purpose = "new", roleIds = new[] { await Role(ApplicationAccessRoles.LegalPersons) } };
        Assert.Equal(HttpStatusCode.Forbidden, (await guest.PostAsJsonAsync("/api/role-requests", body)).StatusCode);
        guest.DefaultRequestHeaders.Remove("X-Email-Verified");
        Assert.Equal(HttpStatusCode.BadRequest, (await guest.PostAsJsonAsync("/api/role-requests", new { clientRequestId = Guid.NewGuid(), purpose = "new", roleIds = new[] { Guid.NewGuid() } })).StatusCode);
        await InDatabase(async db => { Assert.Empty(db.RoleRequests); Assert.Empty(db.Notifications); });
    }

    [Fact]
    public async Task GuestRequestPersistsNothingPrivilegedUntilOtherAdminApprovesMultipleRoles()
    {
        using var applicant = NewClient("new-user");
        var roles = new[] { await Role(ApplicationAccessRoles.LegalPersons), await Role(ApplicationAccessRoles.Limits) };
        var row = await Submit(applicant, roles: roles);
        await InDatabase(async db => Assert.False(await db.AppUsers.AnyAsync(user => user.KeycloakId == "new-user")));
        using var admin = NewClient("reviewer"); var id = row.GetProperty("id").GetGuid();
        var decision = new { decisionId = Guid.NewGuid(), expectedVersion = 1, note = "Approved fixture" };
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/role-requests/{id}/approve", decision)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/role-requests/{id}/approve", decision)).StatusCode);
        await InDatabase(async db =>
        {
            var user = await db.AppUsers.SingleAsync(user => user.KeycloakId == "new-user");
            Assert.True(user.KeycloakSubjectConfirmed); Assert.Equal("new-user@example.test", user.Email);
            Assert.Equal(2, await db.UserRoles.CountAsync(item => item.UserId == user.Id && item.IsActive));
            Assert.Equal(1, await db.AuditLogs.CountAsync(item => item.Action == "ROLE_REQUEST_APPROVED"));
            Assert.Equal(4, await db.Notifications.CountAsync()); Assert.Equal(4, await db.NotificationDeliveries.CountAsync());
        });
    }

    [Fact]
    public async Task SelfDecisionAndStaleDecisionAreRejectedWithoutGrantOrNotice()
    {
        var row = await Submit(Client); var id = row.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden, (await Client.PostAsJsonAsync($"/api/role-requests/{id}/approve", new { decisionId = Guid.NewGuid(), expectedVersion = 1 })).StatusCode);
        using var admin = NewClient("reviewer");
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/role-requests/{id}/approve", new { decisionId = Guid.NewGuid(), expectedVersion = 2 })).StatusCode);
        await InDatabase(async db => { Assert.Equal("pending", (await db.RoleRequests.SingleAsync()).Status); Assert.Equal(3, await db.Notifications.CountAsync()); });
    }

    [Fact]
    public async Task OwnerListsAndInboxDoNotExposeOtherSubjectsEvenToAdmin()
    {
        using var guest = NewClient("guest"); await Submit(guest);
        var mine = await Client.GetFromJsonAsync<JsonElement>("/api/role-requests/mine");
        Assert.Equal(0, mine.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.Forbidden, (await guest.GetAsync("/api/role-requests")).StatusCode);
        var inbox = await Client.GetFromJsonAsync<JsonElement>("/api/notifications");
        var id = inbox.GetProperty("items")[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await guest.PostAsync($"/api/notifications/{id}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/notifications/{id}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/notifications/{id}/read", null)).StatusCode);
    }

    [Fact]
    public async Task RequestAndDecisionReplaysAreIdempotentAndRejectNeverProvisions()
    {
        using var guest = NewClient("guest"); var key = Guid.NewGuid(); var roleId = await Role(ApplicationAccessRoles.LegalPersons);
        var row = await Submit(guest, roles: [roleId], key: key); var id = row.GetProperty("id").GetGuid();
        var replay = await guest.PostAsJsonAsync("/api/role-requests", new { clientRequestId = key, purpose = "new", roleIds = new[] { roleId }, note = "Fixture role request" });
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        var decision = new { decisionId = Guid.NewGuid(), expectedVersion = 1, note = "Rejected fixture" };
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync($"/api/role-requests/{id}/reject", decision)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync($"/api/role-requests/{id}/reject", decision)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PostAsJsonAsync($"/api/role-requests/{id}/approve", decision)).StatusCode);
        await InDatabase(async db => { Assert.False(await db.AppUsers.AnyAsync(user => user.KeycloakId == "guest")); Assert.Equal(1, await db.AuditLogs.CountAsync(item => item.Action == "ROLE_REQUEST_REJECTED")); });
    }

    [Fact]
    public async Task MailFailureAndRetryNeverRepeatGrantsOrDecisionAudit()
    {
        using var guest = NewClient("guest"); var row = await Submit(guest); var id = row.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync($"/api/role-requests/{id}/approve", new { decisionId = Guid.NewGuid(), expectedVersion = 1 })).StatusCode);
        _mail.Setup(mail => mail.SendApplicationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("private transport detail"));
        await using (var scope = App.Services.CreateAsyncScope()) Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<NotificationDispatcher>().DeliverBatchAsync(default));
        await InDatabase(async db => { foreach (var delivery in await db.NotificationDeliveries.ToListAsync()) delivery.NextAttemptAt = DateTime.UtcNow.AddMinutes(-1); await db.SaveChangesAsync(); });
        _mail.Setup(mail => mail.SendApplicationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await using (var scope = App.Services.CreateAsyncScope()) Assert.Equal(4, await scope.ServiceProvider.GetRequiredService<NotificationDispatcher>().DeliverBatchAsync(default));
        await using (var scope = App.Services.CreateAsyncScope()) Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<NotificationDispatcher>().DeliverBatchAsync(default));
        await InDatabase(async db =>
        {
            var user = await db.AppUsers.SingleAsync(user => user.KeycloakId == "guest");
            Assert.Equal(1, await db.UserRoles.CountAsync(item => item.UserId == user.Id && item.IsActive));
            Assert.Equal(1, await db.AuditLogs.CountAsync(item => item.Action == "ROLE_REQUEST_APPROVED"));
            Assert.Equal(4, await db.NotificationDeliveries.CountAsync(item => item.DeliveredAt != null));
        });
    }

    [Fact]
    public async Task RecreatedSubjectCannotInheritConfirmedEmailOwnersPrivileges()
    {
        using var guest = NewClient("recreated"); guest.DefaultRequestHeaders.Add("X-Email", "creator@example.test");
        var row = await Submit(guest); var id = row.GetProperty("id").GetGuid(); using var admin = NewClient("reviewer");
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/role-requests/{id}/approve", new { decisionId = Guid.NewGuid(), expectedVersion = 1 })).StatusCode);
        await InDatabase(async db => Assert.False(await db.AppUsers.AnyAsync(user => user.KeycloakId == "recreated")));
    }

    [Fact]
    public async Task ActiveDatabaseCatalogRoleWorksWithoutAProviderRoleOrHardcodedAllowList()
    {
        var id = Guid.NewGuid();
        await InDatabase(async db =>
        {
            db.Roles.Add(new Role { Id = id, Name = "Fixture custom application role" });
            db.RolePermissions.Add(new RolePermission { RoleId = id, PermissionId = (await db.Permissions.SingleAsync(item => item.Code == ApplicationPermissions.LegalPersonsCreate)).Id });
            await db.SaveChangesAsync();
        });
        using var guest = NewClient("custom-role-user"); var row = await Submit(guest, roles: [id]);
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync($"/api/role-requests/{row.GetProperty("id").GetGuid()}/approve", new { decisionId = Guid.NewGuid(), expectedVersion = 1 })).StatusCode);
        var response = await guest.PostAsJsonAsync("/api/legal-entities", new { name = "Fixture", isResident = false, fbaId = "456", basisOfConnection = "VL", connectionDescription = "Fixture relation", connectedWithBank = true, dateFrom = "2026-01-01" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousAndInactiveIdentitiesCannotUseWorkflowRoutes()
    {
        using var anonymous = NewClient("anonymous");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/role-requests/options")).StatusCode);
        await InDatabase(async db => { (await db.AppUsers.SingleAsync(user => user.KeycloakId == "creator")).IsActive = false; await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.Forbidden, (await Client.GetAsync("/api/role-requests/mine")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client.GetAsync("/api/notifications")).StatusCode);
    }
}
