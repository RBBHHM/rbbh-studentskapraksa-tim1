using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;

namespace IntegrationTests.Tests.Users;

public sealed class RoleRequestWorkflowSqlTests : RoleRequestFixture
{
    protected override bool UseSql => true;

    [SqlServerFact]
    public async Task OptionsUseRoleNameAndExcludeInactiveRoles()
    {
        await InDatabase(async db =>
        {
            db.Roles.AddRange(
                new Role { Name = "fixture-label", CreatedBy = "fixture" },
                new Role { Name = "fixture-fallback", CreatedBy = "fixture" },
                new Role { Name = "fixture-inactive", IsActive = false, CreatedBy = "fixture" });
            await db.SaveChangesAsync();
        });

        using var applicant = NewClient("role-applicant");
        var payload = await applicant.GetFromJsonAsync<JsonElement>("/api/role-requests/options");
        var labels = payload.GetProperty("items").EnumerateArray()
            .ToDictionary(role => role.GetProperty("name").GetString()!, role => role.GetProperty("displayName").GetString());

        Assert.Equal("fixture-label", labels["fixture-label"]);
        Assert.Equal("fixture-fallback", labels["fixture-fallback"]);
        Assert.DoesNotContain("fixture-inactive", labels.Keys);
        Assert.DoesNotContain(ApplicationAccessRoles.Guest, labels.Keys);
    }

    [SqlServerFact]
    public async Task DetailedRoleMailRendersCompleteUnicodeNotesAndRolesWithoutRawHtmlOrDuplicateGrants()
    {
        long[] roles = [];
        await InDatabase(async db =>
        {
            var additions = Enumerable.Range(0, 20).Select(index => new Role { Name = new string('Č', 90) + index, CreatedBy = "fixture" }).ToArray();
            db.Roles.AddRange(additions); await db.SaveChangesAsync(); roles = additions.Select(role => role.Id).ToArray();
        });
        using var guest = NewClient("unicode-user"); var note = "<img src=x onerror=evil()>" + new string('ć', 950);
        var response = await guest.PostAsJsonAsync("/api/role-requests", new { clientRequestId = Guid.NewGuid(), purpose = "new", roleIds = roles, note });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await InDatabase(async db => { Assert.All(db.Notifications, row => Assert.True(row.ParametersJson.Length > 8000)); Assert.Equal("pending", (await db.RoleRequests.SingleAsync()).Status); });
        var html = new List<string>();
        _mail.Setup(mail => mail.SendApplicationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, body, _) => html.Add(body)).Returns(Task.CompletedTask);
        await using (var scope = App.Services.CreateAsyncScope()) Assert.Equal(3, await scope.ServiceProvider.GetRequiredService<NotificationDispatcher>().DeliverBatchAsync(default));
        Assert.Equal(3, html.Count); Assert.All(html, body => { Assert.Contains("unicode-user", body); Assert.Contains("&lt;img", body); Assert.DoesNotContain("<img", body); });
        await InDatabase(async db => { Assert.False(await db.AppUsers.AnyAsync(user => user.KeycloakId == "unicode-user")); Assert.Equal("pending", (await db.RoleRequests.SingleAsync()).Status); });
    }

    [SqlServerFact]
    public async Task CompetingApprovalAndRejectionCommitOneDecisionAndNoDuplicateGrants()
    {
        using var guest = NewClient("new-user"); var row = await Submit(guest); var id = row.GetProperty("id").GetInt64();
        using var other = NewClient("reviewer");
        var decision = new { decisionId = Guid.NewGuid(), expectedVersion = 1 };
        var results = await Task.WhenAll(Client.PostAsJsonAsync($"/api/role-requests/{id}/approve", decision),
            other.PostAsJsonAsync($"/api/role-requests/{id}/reject", decision));
        Assert.Single(results, result => result.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, result => result.StatusCode == HttpStatusCode.Conflict);
        await InDatabase(async db =>
        {
            Assert.Equal(1, await db.AuditLogs.CountAsync(item => item.Action == "ROLE_REQUEST_APPROVED" || item.Action == "ROLE_REQUEST_REJECTED"));
            Assert.Equal(4, await db.Notifications.CountAsync());
            var request = await db.RoleRequests.SingleAsync();
            var user = await db.AppUsers.SingleOrDefaultAsync(user => user.KeycloakId == "new-user");
            if (request.Status == "approved") Assert.Equal(1, await db.UserRoles.CountAsync(item => item.UserId == user!.Id && item.IsActive));
            else Assert.Null(user);
        });
    }

    [SqlServerFact]
    public async Task AuditFailureRollsBackAccountGrantsDecisionAndNotifications()
    {
        using var guest = NewClient("new-user"); var row = await Submit(guest); var id = row.GetProperty("id").GetInt64();
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER FixtureRejectRoleAudit ON AUDIT_LOGS AFTER INSERT AS
            BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action = 'ROLE_REQUEST_APPROVED') THROW 51000, 'fixture audit failure', 1; END
            """));
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.PostAsJsonAsync($"/api/role-requests/{id}/approve", new { decisionId = Guid.NewGuid(), expectedVersion = 1 })).StatusCode);
        await InDatabase(async db =>
        {
            Assert.Equal("pending", (await db.RoleRequests.SingleAsync()).Status);
            Assert.False(await db.AppUsers.AnyAsync(user => user.KeycloakId == "new-user"));
            Assert.Equal(3, await db.Notifications.CountAsync()); Assert.Equal(3, await db.NotificationDeliveries.CountAsync());
            Assert.Equal(1, await db.AuditLogs.CountAsync());
        });
    }

    [SqlServerFact]
    public async Task ParallelDeliveryWorkersClaimEachNoticeOnlyOnce()
    {
        using var guest = NewClient("new-user"); var row = await Submit(guest); var id = row.GetProperty("id").GetInt64();
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync($"/api/role-requests/{id}/approve", new { decisionId = Guid.NewGuid(), expectedVersion = 1 })).StatusCode);
        _mail.Setup(mail => mail.SendApplicationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string to, string title, string body, CancellationToken ct) => Task.Delay(20, ct));
        await using var first = App.Services.CreateAsyncScope(); await using var second = App.Services.CreateAsyncScope();
        var results = await Task.WhenAll(first.ServiceProvider.GetRequiredService<NotificationDispatcher>().DeliverBatchAsync(default),
            second.ServiceProvider.GetRequiredService<NotificationDispatcher>().DeliverBatchAsync(default));
        Assert.Equal(4, results.Sum());
        _mail.Verify(mail => mail.SendApplicationNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [SqlServerFact]
    public async Task WorkflowMigrationRetainsExistingConfirmedUserAndRoleAssignments()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = "RBBH_WORKFLOW_UPGRADE_" + Guid.NewGuid().ToString("N") };
        await using var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            var previous = db.Database.GetMigrations().Single(name => name.EndsWith("_AddPeriodRequestOwnership"));
            await db.GetService<IMigrator>().MigrateAsync(previous);
            var oldUserId = Guid.NewGuid();
            var oldRoleId = Guid.NewGuid();
            var roleName = ApplicationAccessRoles.LegalPersons;
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT dbo.AppUsers (Id, KeycloakId, KeycloakSubjectConfirmed, Username, FirstName, LastName, Email, IsActive, CreatedAt, CreatedBy)
                VALUES ({oldUserId}, {"confirmed-existing-sub"}, 1, {"legacy"}, {"Legacy"}, {"Fixture"}, {"legacy@example.test"}, 1, SYSUTCDATETIME(), N'fixture');
                INSERT dbo.Roles (Id, Name, IsActive, CreatedAt, CreatedBy) VALUES ({oldRoleId}, {roleName}, 1, SYSUTCDATETIME(), N'fixture');
                INSERT dbo.UserRoles (Id, UserId, RoleId, IsActive, CreatedAt, CreatedBy) VALUES (NEWID(), {oldUserId}, {oldRoleId}, 1, SYSUTCDATETIME(), N'fixture');
                """);
            await db.Database.MigrateAsync();
            var user = await db.AppUsers.SingleAsync();
            Assert.True(user.KeycloakSubjectConfirmed);
            Assert.Equal(user.Id, (await db.UserRoles.SingleAsync()).UserId);
            Assert.Equal((await db.Roles.SingleAsync()).Id, (await db.UserRoles.SingleAsync()).RoleId);
            Assert.Single(db.UserRoles); Assert.Empty(db.RoleRequests); Assert.Empty(db.Notifications);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
