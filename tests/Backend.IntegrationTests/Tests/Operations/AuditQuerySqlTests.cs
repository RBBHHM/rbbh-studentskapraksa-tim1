using System.Net;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using IntegrationTests.Tests.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.DL.Entities.Audit;

namespace IntegrationTests.Tests.Operations;

public sealed class AuditQuerySqlTests : AuthenticatedApiFixture
{
    protected override bool UseSql => true;
    private const string Area = "AuditQueryFixture";

    [SqlServerFact]
    public async Task ExtremePageDoesNotOverflowSqlOffsetOrReturnTheFirstPage()
    {
        await Seed();
        using var response = await Client.GetAsync($"/api/audit-logs?tableName={Area}&page={int.MaxValue}&pageSize=200");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(int.MaxValue, body.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(2, body.RootElement.GetProperty("total").GetInt32());
        Assert.Empty(body.RootElement.GetProperty("logs").EnumerateArray());
    }

    [SqlServerFact]
    public async Task FinalCalendarDayIncludesItsLastTimestampWithoutAddingAnUnrepresentableDay()
    {
        await Seed();
        using var response = await Client.GetAsync($"/api/audit-logs?tableName={Area}&dateFrom=9999-12-31&dateTo=9999-12-31");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var row = Assert.Single(body.RootElement.GetProperty("logs").EnumerateArray());
        Assert.Equal("last-day", row.GetProperty("recordId").GetString());
    }

    [SqlServerFact]
    public async Task OrdinaryInclusiveDateFilterAndAdministrativeAuthorizationRemainUnchanged()
    {
        await Seed();
        using var response = await Client.GetAsync($"/api/audit-logs?tableName={Area}&dateFrom=2026-11-01&dateTo=2026-11-01");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var row = Assert.Single(body.RootElement.GetProperty("logs").EnumerateArray());
        Assert.Equal("ordinary-day", row.GetProperty("recordId").GetString());
        using var guest = NewClient("unmapped-guest");
        using var denied = await guest.GetAsync("/api/audit-logs");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var anonymous = NewClient("anonymous");
        using var unauthenticated = await anonymous.GetAsync("/api/audit-logs");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        await InDatabase(db =>
        {
            Assert.Equal(2, db.AuditLogs.Count(row => row.TableName == Area));
            return Task.CompletedTask;
        });
    }

    [SqlServerFact]
    public async Task UpgradePreservesAuditHistoryAndUsesNumericKeysWithoutIpAddresses()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = "RBBH_AUDIT_KEYS_" + Guid.NewGuid().ToString("N") };
        await using var db = new ConnectedPartiesDbContext(
            new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20261124140633_MoveLegalEntityVerificationOrigin");
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO dbo.AuditLogs (Id, TableName, RecordId, Action, Username, Timestamp, IpAddress, OldValues)
                VALUES (NEWID(), N'AuditQueryFixture', N'legacy', N'UPDATE', N'creator', SYSUTCDATETIME(), N'192.0.2.15', N'{{"old":true}}');
                """);

            await db.Database.MigrateAsync();
            var historical = await db.AuditLogs.SingleAsync();
            Assert.True(historical.Id > 0);
            Assert.Equal("legacy", historical.RecordId);
            using var snapshot = System.Text.Json.JsonDocument.Parse(historical.OldValues!);
            Assert.True(snapshot.RootElement.GetProperty("old").GetBoolean());

            var next = new AuditLog { TableName = Area, RecordId = "fresh", Action = "INSERT", Username = "creator" };
            db.AuditLogs.Add(next);
            await db.SaveChangesAsync();
            Assert.True(next.Id > historical.Id);
            Assert.Equal(0, await db.Database.SqlQueryRaw<int>("""
                SELECT COUNT(*) AS [Value] FROM sys.columns
                WHERE (object_id = OBJECT_ID(N'dbo.AUDIT_LOGS') AND name IN (N'IP_ADDRESS', N'RECORD_ID'))
                   OR (object_id = OBJECT_ID(N'dbo.PERMISSIONS') AND name = N'LABEL')
                """).SingleAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    private Task Seed() => InDatabase(async db =>
    {
        db.AuditLogs.AddRange(
            new AuditLog { TableName = Area, RecordId = "ordinary-day", Action = "INSERT",
                Username = "creator", Timestamp = new DateTime(2026, 11, 1, 23, 59, 59, DateTimeKind.Utc) },
            new AuditLog { TableName = Area, RecordId = "last-day", Action = "INSERT",
                Username = "creator", Timestamp = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc) });
        await db.SaveChangesAsync();
    });
}
