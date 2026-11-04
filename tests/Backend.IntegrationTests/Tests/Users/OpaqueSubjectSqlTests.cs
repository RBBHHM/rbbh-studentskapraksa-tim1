using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using RBBH.ConnectedParties.DL.Entities.Notifications;

namespace IntegrationTests.Tests.Users;

public sealed class OpaqueSubjectSqlTests : RoleRequestFixture
{
    protected override bool UseSql => true;

    [SqlServerFact]
    public async Task SubjectCaseVariantCannotInheritAdministratorPermissions()
    {
        using var variant = NewClient("CREATOR");
        Assert.Equal(HttpStatusCode.Forbidden, (await variant.GetAsync("/api/role-requests")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Client.GetAsync("/api/role-requests")).StatusCode);
    }


    [SqlServerFact]
    public async Task CaseVariantCannotReadOrRespondToAnotherPeriodRequest()
    {
        // Both identities have legitimate Capital grants; authorization alone must
        // not hide an owner predicate that compares subjects incorrectly in SQL.
        await InDatabase(async db =>
        {
            var user = new RBBH.ConnectedParties.DL.Entities.Users.AppUser { KeycloakId = "CREATOR", KeycloakSubjectConfirmed = true,
                Username = "separate-admin", Email = "separate-admin@example.test" };
            db.AppUsers.Add(user);
            db.UserRoles.Add(new() { UserId = user.Id, RoleId = (await db.Roles.SingleAsync(role => role.Name == "admin")).Id });
            db.UnlockRequests.Add(new() { RequestedBySubject = "creator", RequestedBy = "creator", RequestedByEmail = "creator@example.test",
                Year = 2026, Month = 10, Reason = "Original request reason", Status = "NEEDS_INFO", CreatedBy = "fixture" });
            await db.SaveChangesAsync();
        });
        using var variant = NewClient("CREATOR");
        var list = await variant.GetFromJsonAsync<JsonElement>("/api/period-lock/unlock-requests");
        Assert.Equal(0, list.GetProperty("total").GetInt32());
        Guid id = Guid.Empty;
        await InDatabase(async db => id = (await db.UnlockRequests.SingleAsync()).Id);
        Assert.Equal(HttpStatusCode.NotFound, (await variant.PostAsJsonAsync($"/api/period-lock/unlock-requests/{id}/respond", new { message = "Response from another subject" })).StatusCode);
        await InDatabase(async db => { Assert.Single(db.UnlockRequests); Assert.Empty(db.AuditLogs); });
    }

    [SqlServerFact]
    public async Task MigrationPreservesPopulatedOpaqueIdentitiesAndOwnershipWithoutBackfill()
    {
        var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = "RBBH_OPAQUE_UPGRADE_" + Guid.NewGuid().ToString("N") };
        await using var db = new RBBH.ConnectedParties.DL.Persistence.ConnectedPartiesDbContext(
            new DbContextOptionsBuilder<RBBH.ConnectedParties.DL.Persistence.ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>()
                .MigrateAsync("20261103075242_AddPeriodConcurrencyAndNotificationResources");
            var user = new RBBH.ConnectedParties.DL.Entities.Users.AppUser { KeycloakId = "Original-Subject", KeycloakSubjectConfirmed = true,
                Username = "original", Email = "original@example.test" };
            var request = new RBBH.ConnectedParties.DL.Entities.Authorization.RoleRequest { RequesterSubject = user.KeycloakId, ClientRequestId = Guid.NewGuid(), Email = user.Email };
            var notice = new ApplicationNotification { RecipientSubject = user.KeycloakId, EventKey = "retained" };
            var period = new RBBH.ConnectedParties.DL.Entities.PeriodLock.UnlockRequest { RequestedBySubject = user.KeycloakId,
                RequestedBy = user.Username, RequestedByEmail = user.Email, Reason = "Historical request reason", Year = 2026, Month = 10 };
            db.AddRange(user, request, notice, period); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            await db.Database.MigrateAsync();
            Assert.Equal(user.Id, (await db.AppUsers.SingleAsync()).Id);
            Assert.Equal(user.KeycloakId, (await db.AppUsers.SingleAsync()).KeycloakId);
            Assert.Equal(user.KeycloakId, (await db.RoleRequests.SingleAsync()).RequesterSubject);
            Assert.Equal(user.KeycloakId, (await db.Notifications.SingleAsync()).RecipientSubject);
            Assert.Equal(user.KeycloakId, (await db.UnlockRequests.SingleAsync()).RequestedBySubject);
            var variant = user.KeycloakId.ToUpperInvariant();
            Assert.False(await db.AppUsers.AnyAsync(row => row.KeycloakId == variant));
            Assert.False(await db.RoleRequests.AnyAsync(row => row.RequesterSubject == variant));
            Assert.False(await db.Notifications.AnyAsync(row => row.RecipientSubject == variant));
            Assert.False(await db.UnlockRequests.AnyAsync(row => row.RequestedBySubject == variant));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    [SqlServerFact]
    public async Task SubjectWhitespaceIsNeverNormalizedIntoAConfirmedIdentity()
    {
        await InDatabase(async db =>
        {
            foreach (var subject in new[] { " creator", "creator ", "", new string('x', 101) })
            {
                var principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
                { new System.Security.Claims.Claim("sub", subject), new System.Security.Claims.Claim("email", "creator@example.test") }, "fixture"));
                var result = await new RBBH.ConnectedParties.IoC.Extensions.Authentication.ApplicationIdentityResolver(db).ResolveAsync(principal);
                Assert.True(result.Denied); Assert.Null(result.User);
            }
        });
    }

    [SqlServerFact]
    public async Task SubjectCaseVariantCannotReadOrMutateAnotherInboxOrRequests()
    {
        using var owner = NewClient("opaque-owner");
        var request = await Submit(owner);
        Guid noticeId = Guid.NewGuid();
        await InDatabase(async db =>
        {
            db.Notifications.Add(new ApplicationNotification { Id = noticeId, RecipientSubject = "opaque-owner", EventKey = "case-proof", TitleKey = "title", BodyKey = "body", ParametersJson = "{}" });
            await db.SaveChangesAsync();
        });
        using var variant = NewClient("OPAQUE-OWNER");
        var mine = await variant.GetFromJsonAsync<JsonElement>("/api/role-requests/mine");
        Assert.Equal(0, mine.GetProperty("total").GetInt32());
        var inbox = await variant.GetFromJsonAsync<JsonElement>("/api/notifications");
        Assert.Equal(0, inbox.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await variant.PostAsync($"/api/notifications/{noticeId}/read", null)).StatusCode);
        var original = await owner.GetFromJsonAsync<JsonElement>("/api/role-requests/mine");
        Assert.Equal(request.GetProperty("id").GetGuid(), original.GetProperty("items")[0].GetProperty("id").GetGuid());
        await InDatabase(async db => Assert.Null((await db.Notifications.SingleAsync(row => row.Id == noticeId)).ReadAt));
    }
}
