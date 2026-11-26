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
        long id = 0;
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
            const string subject = "Original-Subject";
            const string email = "original@example.test";
            var oldRequestId = Guid.NewGuid();
            var oldNoticeId = Guid.NewGuid();
            var parameters = JsonSerializer.Serialize(new { requestId = oldRequestId });
            var route = $"/app/profile?requestId={oldRequestId}";
            var eventKey = $"role-request:{oldRequestId}:approved";
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT dbo.AppUsers (Id, KeycloakId, KeycloakSubjectConfirmed, Username, FirstName, LastName, Email, IsActive, CreatedAt, CreatedBy)
                VALUES (NEWID(), {subject}, 1, N'original', N'Original', N'Fixture', {email}, 1, SYSUTCDATETIME(), N'fixture');
                INSERT dbo.RoleRequests (Id, ClientRequestId, RequesterSubject, Username, FirstName, LastName, Email, Purpose, Status, CreatedAt, Version)
                VALUES ({oldRequestId}, NEWID(), {subject}, N'original', N'Original', N'Fixture', {email}, N'change', N'approved', SYSUTCDATETIME(), 2);
                INSERT dbo.Notifications (Id, RecipientSubject, RecipientEmail, EventKey, TitleKey, BodyKey, ParametersJson, Route, CreatedAt)
                VALUES ({oldNoticeId}, {subject}, {email}, {eventKey}, N'title', N'body', {parameters}, {route}, SYSUTCDATETIME());
                INSERT dbo.NotificationDeliveries (Id, NotificationId, Attempts, NextAttemptAt) VALUES (NEWID(), {oldNoticeId}, 0, SYSUTCDATETIME());
                INSERT dbo.UnlockRequests (Id, Year, Month, Reason, RequestedBySubject, RequestedBy, RequestedByEmail, Status, CreatedAt, CreatedBy, IsActive)
                VALUES (NEWID(), 2026, 10, N'Historical request reason', {subject}, N'original', {email}, N'PENDING', SYSUTCDATETIME(), N'fixture', 1);
                """);

            await db.Database.MigrateAsync();
            Assert.Equal(subject, (await db.AppUsers.SingleAsync()).KeycloakId);
            var migratedRequest = await db.RoleRequests.SingleAsync();
            Assert.True(migratedRequest.Id > 0);
            Assert.Equal(subject, migratedRequest.RequesterSubject);
            var notice = await db.Notifications.Include(row => row.Delivery).SingleAsync();
            Assert.Equal(subject, notice.RecipientSubject);
            Assert.Equal(notice.Id, notice.Delivery!.NotificationId);
            Assert.Equal($"/app/profile?requestId={migratedRequest.Id}", notice.Route);
            using var payload = JsonDocument.Parse(notice.ParametersJson);
            Assert.Equal(migratedRequest.Id, payload.RootElement.GetProperty("requestId").GetInt64());
            Assert.Equal(subject, (await db.UnlockRequests.SingleAsync()).RequestedBySubject);

            var nextRequest = new RBBH.ConnectedParties.DL.Entities.Authorization.RoleRequest
            {
                ClientRequestId = Guid.NewGuid(), RequesterSubject = subject, Email = email, Status = "pending"
            };
            db.RoleRequests.Add(nextRequest);
            await db.SaveChangesAsync();
            Assert.True(nextRequest.Id > migratedRequest.Id);
            Assert.Equal(0, await db.Database.SqlQueryRaw<int>("""
                SELECT COUNT(*) AS [Value]
                FROM sys.columns AS col
                INNER JOIN sys.tables AS tbl ON tbl.object_id = col.object_id
                WHERE tbl.schema_id = SCHEMA_ID(N'dbo')
                  AND col.system_type_id = TYPE_ID(N'uniqueidentifier')
                  AND (EXISTS (
                    SELECT 1 FROM sys.indexes AS idx
                    INNER JOIN sys.index_columns AS keycol ON keycol.object_id = idx.object_id AND keycol.index_id = idx.index_id
                    WHERE idx.is_primary_key = 1 AND keycol.object_id = col.object_id AND keycol.column_id = col.column_id
                  ) OR EXISTS (
                    SELECT 1 FROM sys.foreign_key_columns AS fk
                    WHERE fk.parent_object_id = col.object_id AND fk.parent_column_id = col.column_id
                  ))
                """).SingleAsync());
            var variant = subject.ToUpperInvariant();
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
        const long noticeId = 100;
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
        Assert.Equal(request.GetProperty("id").GetInt64(), original.GetProperty("items")[0].GetProperty("id").GetInt64());
        await InDatabase(async db => Assert.Null((await db.Notifications.SingleAsync(row => row.Id == noticeId)).ReadAt));
    }
}
