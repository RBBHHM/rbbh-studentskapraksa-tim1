using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;

namespace IntegrationTests.Tests;

public abstract class CapitalHistoryFixture : AuthenticatedApiFixture
{
    protected override void ConfigureServices(IServiceCollection services)
    { services.AddScoped<ICapitalService, CapitalService>(); services.AddScoped<ILimitService, LimitService>(); }
    protected async Task<JsonElement> Save(DateTime date, decimal amount)
    {
        var response = await Client.PostAsJsonAsync("/api/capital", new { regulatoryCapital = amount, coreCapital = amount / 2, supplementaryCapital = amount / 4, validTill = date });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}

public sealed class CapitalHistoryHttpTests : CapitalHistoryFixture
{
    [Fact]
    public async Task CreatingCapitalClosesTheOldRangeAndOnlyTheNewActiveRowCanBeEdited()
    {
        var today = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Europe/Sarajevo").Date;
        await InDatabase(async db =>
        {
            db.Capitals.Add(new() { CapitalDate = today.AddMonths(-1), RegulatoryCapital = 100, CreatedBy = "original" });
            await db.SaveChangesAsync();
        });

        var created = await Save(today.AddMonths(1), 200);
        var id = created.GetProperty("id").GetInt64();
        var actual = await Client.GetFromJsonAsync<JsonElement>($"/api/capital/as-of?date={today:yyyy-MM-dd}");
        Assert.Equal(id, actual.GetProperty("id").GetInt64());
        var yesterday = await Client.GetFromJsonAsync<JsonElement>($"/api/capital/as-of?date={today.AddDays(-1):yyyy-MM-dd}");
        Assert.Equal(100, yesterday.GetProperty("regulatoryCapital").GetDecimal());
        var afterExpiry = await Client.GetAsync($"/api/capital/as-of?date={today.AddMonths(1).AddDays(1):yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.NoContent, afterExpiry.StatusCode);

        var body = new { regulatoryCapital = 240, coreCapital = 100, supplementaryCapital = 50,
            validTill = today.AddMonths(1), expectedId = id };
        var saved = await Client.PutAsJsonAsync("/api/capital", body);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PutAsJsonAsync("/api/capital", body)).StatusCode);
        var history = await Client.GetFromJsonAsync<JsonElement>("/api/capital/history");
        Assert.Equal(1, history.GetProperty("total").GetInt32());
        await InDatabase(async db =>
        {
            Assert.Equal(2, await db.Capitals.CountAsync());
            Assert.Equal(240, (await db.Capitals.SingleAsync(row => row.IsActive)).RegulatoryCapital);
            Assert.Equal(2, await db.AuditLogs.CountAsync());
        });
    }
    [Fact]
    public async Task LimitReadsUseRequestedHistoricalCapitalAndUnknownRemainsNull()
    {

        await InDatabase(async db =>
        {
            db.Capitals.AddRange(new() { CapitalDate = new(2026, 1, 1), ValidTill = new(2026, 5, 31), RegulatoryCapital = 100, IsActive = false },
                new() { CapitalDate = new(2026, 6, 1), RegulatoryCapital = 200 });
            var legal = new RBBH.ConnectedParties.DL.Entities.LegalEntity.LegalEntity { Name = "Fixture", CreatedBy = "fixture" }; db.LegalEntities.Add(legal);
            db.Limiti.Add(new() { LegalEntityId = legal.Id, TipLimita = "TEST", CreatedBy = "fixture" }); await db.SaveChangesAsync();
        });
        var january = await Client.GetFromJsonAsync<JsonElement>("/api/limiti?asOf=2026-02-01"); Assert.Equal(100, january[0].GetProperty("regulatorniKapital").GetDecimal());
        var june = await Client.GetFromJsonAsync<JsonElement>("/api/limiti/page?asOf=2026-06-15"); Assert.Equal(200, june.GetProperty("items")[0].GetProperty("regulatorniKapital").GetDecimal());
        var unknown = await Client.GetFromJsonAsync<JsonElement>("/api/limiti?asOf=2025-12-31"); Assert.Equal(JsonValueKind.Null, unknown[0].GetProperty("regulatorniKapital").ValueKind);
    }
    [Theory]
    [InlineData(-1)]
    [InlineData(1.111)]
    public async Task InvalidAmountsDoNotCreateRevisionOrAudit(decimal amount)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PutAsJsonAsync("/api/capital", new { regulatoryCapital = amount, coreCapital = 10, supplementaryCapital = 10, capitalDate = "2026-01-01" })).StatusCode);
        await InDatabase(async db => { Assert.Empty(db.Capitals); Assert.Empty(db.AuditLogs); });
    }
    [Fact]
    public async Task LockedPeriodAndMissingOrMalformedDateDoNotBypassExistingAuthorization()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync("/api/capital/as-of")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync("/api/capital/as-of?date=not-a-date")).StatusCode);
        await InDatabase(async db => { var row = await db.PeriodLocks.SingleAsync(); row.IsLocked = true; await db.SaveChangesAsync(); });
        Assert.Equal((HttpStatusCode)423, (await Client.PutAsJsonAsync("/api/capital", new { regulatoryCapital = 100, coreCapital = 50, supplementaryCapital = 25, capitalDate = "2026-01-01" })).StatusCode);
        await InDatabase(async db => { Assert.Empty(db.Capitals); Assert.Empty(db.AuditLogs); });
    }
}
