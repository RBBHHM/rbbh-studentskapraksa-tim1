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
        var response = await Client.PutAsJsonAsync("/api/capital", new { regulatoryCapital = amount, coreCapital = amount / 2, supplementaryCapital = amount / 4, capitalDate = date });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}

public sealed class CapitalHistoryHttpTests : CapitalHistoryFixture
{
    [Fact]
    public async Task EffectiveDatesAndSameDateCorrectionsAppendWithoutInventingOlderValues()
    {
        await InDatabase(async db => { db.Capitals.Add(new() { CapitalDate = new(2026, 1, 1), RegulatoryCapital = 100, CoreCapital = 50, SupplementaryCapital = 25, CreatedBy = "original" }); await db.SaveChangesAsync(); });
        var first = await Save(new(2026, 2, 1), 200); var correction = await Save(new(2026, 2, 1), 240); await Save(new(2027, 1, 1), 400);
        Assert.NotEqual(first.GetProperty("id").GetGuid(), correction.GetProperty("id").GetGuid());
        var before = await Client.GetAsync("/api/capital/as-of?date=2025-12-31");
        Assert.Equal(HttpStatusCode.NoContent, before.StatusCode); Assert.Equal("", await before.Content.ReadAsStringAsync());
        var january = await Client.GetFromJsonAsync<JsonElement>("/api/capital/as-of?date=2026-01-31"); Assert.Equal(100, january.GetProperty("regulatoryCapital").GetDecimal());
        var march = await Client.GetFromJsonAsync<JsonElement>("/api/capital/as-of?date=2026-03-01"); Assert.Equal(240, march.GetProperty("regulatoryCapital").GetDecimal());
        var future = await Client.GetFromJsonAsync<JsonElement>("/api/capital/as-of?date=2027-01-01"); Assert.Equal(400, future.GetProperty("regulatoryCapital").GetDecimal());
        var history = await Client.GetFromJsonAsync<JsonElement>("/api/capital/history?page=1&pageSize=100"); Assert.Equal(4, history.GetProperty("total").GetInt32());
        Assert.Single(history.GetProperty("items").EnumerateArray(), row => row.GetProperty("source").GetString() == "legacy");
        await InDatabase(async db => { Assert.Equal(100, (await db.Capitals.SingleAsync()).RegulatoryCapital); Assert.Equal(3, await db.CapitalRevisions.CountAsync()); Assert.Equal(3, await db.AuditLogs.CountAsync()); });
    }
    [Fact]
    public async Task LimitReadsUseRequestedHistoricalCapitalAndUnknownRemainsNull()
    {
        await Save(new(2026, 1, 1), 100); await Save(new(2026, 6, 1), 200);
        await InDatabase(async db =>
        {
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
        await InDatabase(async db => { Assert.Empty(db.CapitalRevisions); Assert.Empty(db.AuditLogs); });
    }
    [Fact]
    public async Task LockedPeriodAndMissingOrMalformedDateDoNotBypassExistingAuthorization()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync("/api/capital/as-of")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.GetAsync("/api/capital/as-of?date=not-a-date")).StatusCode);
        await InDatabase(async db => { var row = await db.PeriodLocks.SingleAsync(); row.IsLocked = true; await db.SaveChangesAsync(); });
        Assert.Equal((HttpStatusCode)423, (await Client.PutAsJsonAsync("/api/capital", new { regulatoryCapital = 100, coreCapital = 50, supplementaryCapital = 25, capitalDate = "2026-01-01" })).StatusCode);
        await InDatabase(async db => { Assert.Empty(db.CapitalRevisions); Assert.Empty(db.AuditLogs); });
    }
}
