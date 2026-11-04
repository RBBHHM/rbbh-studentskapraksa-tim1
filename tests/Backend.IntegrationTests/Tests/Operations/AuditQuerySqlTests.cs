using System.Net;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using IntegrationTests.Tests.Users;
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

    private Task Seed() => InDatabase(async db =>
    {
        db.AuditLogs.AddRange(
            new AuditLog { TableName = Area, RecordId = "ordinary-day", Action = "INSERT", UserId = "creator",
                Username = "creator", Timestamp = new DateTime(2026, 11, 1, 23, 59, 59, DateTimeKind.Utc) },
            new AuditLog { TableName = Area, RecordId = "last-day", Action = "INSERT", UserId = "creator",
                Username = "creator", Timestamp = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc) });
        await db.SaveChangesAsync();
    });
}
