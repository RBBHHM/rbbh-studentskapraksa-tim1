using System.Net;
using System.Net.Http.Json;
using IntegrationTests.Tests.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.Extensions.DependencyInjection;
using RBBH.ConnectedParties.BL.Services;

namespace IntegrationTests.Tests.PeriodLock;

public sealed class PeriodAtomicSqlTests : PeriodAtomicFixture
{
    protected override bool UseSql => true;
    [SqlServerFact]
    public async Task LockAuditFailureRollsBackStateAndAllBroadcastDeliveries()
    {
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FixturePeriodAuditFailure ON AuditLogs AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action = 'PERIOD_LOCK') THROW 51000, 'fixture failure', 1; END"));
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.PostAsJsonAsync("/api/period-lock/lock", new { year = DateTime.UtcNow.Year, month = DateTime.UtcNow.Month })).StatusCode);
        await InDatabase(async db => { Assert.False((await db.PeriodLocks.SingleAsync()).IsLocked); Assert.Empty(db.AuditLogs); Assert.Empty(db.Notifications); Assert.Empty(db.NotificationDeliveries); });
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("DROP TRIGGER FixturePeriodAuditFailure"));
        Assert.Equal(HttpStatusCode.OK, (await Client.PostAsJsonAsync("/api/period-lock/lock", new { year = DateTime.UtcNow.Year, month = DateTime.UtcNow.Month })).StatusCode);
        await InDatabase(async db => { Assert.True((await db.PeriodLocks.SingleAsync()).IsLocked); Assert.Single(db.AuditLogs); Assert.Equal(3, await db.Notifications.CountAsync()); });
    }
    [SqlServerFact]
    public async Task UnlockAuditFailureRetainsLockPendingRequestsAndExistingOutbox()
    {
        await Client.PostAsJsonAsync("/api/period-lock/lock", new { year = 2026, month = 10 }); await Request();
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FixturePeriodAuditFailure ON AuditLogs AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action = 'PERIOD_UNLOCK') THROW 51000, 'fixture failure', 1; END"));
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.PostAsJsonAsync("/api/period-lock/unlock", new { year = 2026, month = 10 })).StatusCode);
        await InDatabase(async db => { Assert.True((await db.PeriodLocks.SingleAsync(row => row.Year == 2026 && row.Month == 10)).IsLocked); Assert.Equal("PENDING", (await db.UnlockRequests.SingleAsync()).Status); Assert.Equal(2, await db.AuditLogs.CountAsync()); Assert.Equal(4, await db.Notifications.CountAsync()); });
    }
    [SqlServerFact]
    public async Task RequestAuditFailureDoesNotLeaveARequestOrEmailQueued()
    {
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FixturePeriodAuditFailure ON AuditLogs AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action = 'UNLOCK_REQUEST') THROW 51000, 'fixture failure', 1; END"));
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.PostAsJsonAsync("/api/period-lock/request-unlock", new { year = 2026, month = 10, reason = "Complete request reason" })).StatusCode);
        await InDatabase(async db => { Assert.Empty(db.UnlockRequests); Assert.Empty(db.AuditLogs); Assert.Empty(db.Notifications); });
    }
    [SqlServerFact]
    public async Task ConcurrentRejectAndInfoCommitOnlyOneTransitionAuditAndOwnerNotice()
    {
        var id = await Request(); using var other = NewClient("reviewer");
        var responses = await Task.WhenAll(Client.PostAsJsonAsync($"/api/period-lock/unlock-requests/{id}/reject", new { note = "Reject after review" }),
            other.PostAsJsonAsync($"/api/period-lock/unlock-requests/{id}/request-info", new { note = "More information needed" }));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.All(responses.Where(response => response.StatusCode != HttpStatusCode.OK), response => Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Conflict, HttpStatusCode.NotFound }));
        await InDatabase(async db => { Assert.Contains((await db.UnlockRequests.SingleAsync()).Status, new[] { "REJECTED", "NEEDS_INFO" }); Assert.Equal(2, await db.AuditLogs.CountAsync()); Assert.Equal(2, await db.Notifications.CountAsync()); });
    }
    [SqlServerFact]
    public async Task RepeatedSystemCheckDoesNotDuplicateStateAuditOrBroadcast()
    {
        await using (var scope = App.Services.CreateAsyncScope()) await scope.ServiceProvider.GetRequiredService<PeriodWorkflow>().SetSystemLock(DateTime.UtcNow.Year, DateTime.UtcNow.Month, default);
        await using (var scope = App.Services.CreateAsyncScope()) await scope.ServiceProvider.GetRequiredService<PeriodWorkflow>().SetSystemLock(DateTime.UtcNow.Year, DateTime.UtcNow.Month, default);
        await InDatabase(async db => { Assert.True((await db.PeriodLocks.SingleAsync()).IsLocked); Assert.Single(db.AuditLogs); Assert.Equal(3, await db.Notifications.CountAsync()); Assert.All(db.AuditLogs, row => Assert.Equal("system", row.Username)); });
    }
    [SqlServerFact]
    public async Task AdditiveMigrationPreservesExistingLockRequestAndEditedResourceLabel()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = "RBBH_PERIOD_UPGRADE_" + Guid.NewGuid().ToString("N") };
        await using var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync(db.Database.GetMigrations().Single(name => name.EndsWith("_AddLimitImportIdempotency")));
            await db.Database.ExecuteSqlRawAsync("INSERT PeriodLocks (Id,Year,Month,IsLocked,LockedBy,CreatedBy,CreatedAt,IsActive) VALUES (NEWID(),2026,10,1,'original','original',SYSUTCDATETIME(),1)");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT UnlockRequests (Id,RequestedBy,RequestedByEmail,Year,Month,Reason,Status,CreatedBy,CreatedAt,IsActive,RequestedBySubject) VALUES ({Guid.NewGuid()},{"original"},{"original@example.test"},{2026},{10},{new string('X', 500)},{"NEEDS_INFO"},{"original"},{DateTime.UtcNow},{true},{"proven-subject"})");
            await db.Database.ExecuteSqlRawAsync("UPDATE CodeLists SET ResourceLabel = N'Operator maintained label' WHERE Kategorija = 'LanguagePacks' AND LanguageCode = 'BS' AND ResourceKey = 'errors.request.invalid.title'");
            await db.Database.MigrateAsync(); var period = await db.PeriodLocks.SingleAsync(); Assert.True(period.IsLocked); Assert.Equal("original", period.LockedBy); Assert.NotNull(period.Version);
            var request = await db.UnlockRequests.SingleAsync(); Assert.Equal("NEEDS_INFO", request.Status); Assert.Equal(new string('X', 500), request.Reason); Assert.Equal("proven-subject", request.RequestedBySubject);
            Assert.Equal("Operator maintained label", (await db.CodeLists.SingleAsync(row => row.LanguageCode == "BS" && row.ResourceKey == "errors.request.invalid.title")).ResourceLabel);
            Assert.Empty(db.AuditLogs); Assert.Empty(db.Notifications);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
