using System.Net;
using System.Net.Http.Json;
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
    public async Task CompetingApprovalAndRejectionCommitOneDecisionAndNoDuplicateGrants()
    {
        using var guest = NewClient("new-user"); var row = await Submit(guest); var id = row.GetProperty("id").GetGuid();
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
        using var guest = NewClient("new-user"); var row = await Submit(guest); var id = row.GetProperty("id").GetGuid();
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER FixtureRejectRoleAudit ON AuditLogs AFTER INSERT AS
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
        using var guest = NewClient("new-user"); var row = await Submit(guest); var id = row.GetProperty("id").GetGuid();
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
            var user = new AppUser { Username = "legacy", Email = "legacy@example.test", KeycloakId = "confirmed-existing-sub", KeycloakSubjectConfirmed = true };
            var role = new Role { Name = ApplicationAccessRoles.LegalPersons };
            db.AppUsers.Add(user); db.Roles.Add(role); db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            await db.SaveChangesAsync(); await db.Database.MigrateAsync();
            Assert.Equal(user.Id, (await db.AppUsers.SingleAsync()).Id); Assert.True((await db.AppUsers.SingleAsync()).KeycloakSubjectConfirmed);
            Assert.Single(db.UserRoles); Assert.Empty(db.RoleRequests); Assert.Empty(db.Notifications);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
