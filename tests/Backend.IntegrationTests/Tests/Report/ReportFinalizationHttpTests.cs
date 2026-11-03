using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.Exceptions;

namespace IntegrationTests.Tests.Report;

public abstract class ReportFinalizationFixture : AuthenticatedApiFixture
{
    protected readonly Mock<IFinanceMonthlySource> Finance = new();
    protected override void ConfigureServices(IServiceCollection services)
    { services.AddScoped<IReportService, ReportService>(); services.AddSingleton(Finance.Object); }
    protected async Task SeedExposure()
    {
        await InDatabase(async db =>
        {
            var legal = new RBBH.ConnectedParties.DL.Entities.LegalEntity.LegalEntity { Name = "Fixture company", Matbroj = "001", CreatedBy = "fixture" }; db.LegalEntities.Add(legal);
            db.ClientLimits.Add(new() { LegalEntityId = legal.Id, Currency = "BAM", ExposureLimit = 150, CurrentExposure = 1, RegulatoryCapital = 99, CoreCapital = 88, CreatedBy = "fixture" });
            db.Capitals.Add(new() { CapitalDate = new(2026, 1, 1), RegulatoryCapital = 1000, CoreCapital = 500, CreatedBy = "fixture" });
            await db.SaveChangesAsync();
        });
    }
}

public sealed class ReportFinalizationHttpTests : ReportFinalizationFixture
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(9999, 1)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public async Task InvalidPeriodIs400AndDoesNotPersist(int year, int month)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Client.PostAsync($"/api/reports/monthly/{year}/{month}", null)).StatusCode);
        await InDatabase(async db => { Assert.Empty(db.Reports); Assert.Empty(db.AuditLogs); });
    }
    [Fact]
    public async Task LocalSnapshotIsExplicitlyNonAuthoritativeAndListPagingIsBoundedAndStable()
    {
        await SeedExposure(); var first = await Client.PostAsync("/api/reports/daily", null); Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var dto = await first.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("local.clientlimits", dto.GetProperty("dataSource").GetString()); Assert.False(dto.GetProperty("isAuthoritative").GetBoolean());
        await Client.PostAsync("/api/reports/daily", null);
        var page = await Client.GetFromJsonAsync<JsonElement>("/api/reports/daily?page=-1&pageSize=2147483647"); Assert.Equal(1, page.GetProperty("page").GetInt32()); Assert.Equal(200, page.GetProperty("pageSize").GetInt32());
        var repeat = await Client.GetFromJsonAsync<JsonElement>("/api/reports/daily?page=1&pageSize=200"); Assert.Equal(page.GetProperty("items").GetRawText(), repeat.GetProperty("items").GetRawText());
        var beyond = await Client.GetFromJsonAsync<JsonElement>("/api/reports/daily?page=2147483647&pageSize=200"); Assert.Empty(beyond.GetProperty("items").EnumerateArray());
        await InDatabase(async db => { Assert.Equal(2, await db.Reports.CountAsync()); Assert.Equal(2, await db.AuditLogs.CountAsync()); Assert.All(db.AuditLogs, row => Assert.Equal("creator", row.Username)); });
    }
    [Fact]
    public async Task ConfiguredMonthlySourceUsesDocumentedComponentsAndHistoricalCapitalThenPersistsSnapshot()
    {
        await SeedExposure(); Finance.SetupGet(source => source.Enabled).Returns(true);
        Finance.Setup(source => source.Read(2026, 9, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new FinanceExposure("001", 125, 75) });
        var response = await Client.PostAsync("/api/reports/monthly/2026/9", null); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(200, dto.GetProperty("totalExposure").GetDecimal()); Assert.True(dto.GetProperty("isAuthoritative").GetBoolean());
        var id = dto.GetProperty("id").GetGuid();
        await InDatabase(async db =>
        {
            var report = await db.Reports.SingleAsync(); using var snapshot = JsonDocument.Parse(report.DataSnapshot!);
            Assert.Equal(1000, snapshot.RootElement[0].GetProperty("RegulatoryCapital").GetDecimal()); Assert.Equal(200, snapshot.RootElement[0].GetProperty("CurrentExposure").GetDecimal());
            var source = await db.ClientLimits.SingleAsync(); source.CurrentExposure = 999; await db.SaveChangesAsync();
        });
        var bytes = await Client.GetByteArrayAsync($"/api/reports/export/generated/{id}");
        using var stream = new MemoryStream(bytes); using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        Assert.Contains(workbook.Worksheet(1).CellsUsed(), cell => cell.DataType == ClosedXML.Excel.XLDataType.Number && cell.GetDouble() == 200);
        Assert.DoesNotContain(workbook.Worksheet(1).CellsUsed(), cell => cell.DataType == ClosedXML.Excel.XLDataType.Number && cell.GetDouble() == 999);
    }
    [Fact]
    public async Task SnapshotTotalCannotBeSilentlyRoundedOrOverflowPersistedPrecision()
    {
        await SeedExposure(); Finance.SetupGet(source => source.Enabled).Returns(true);
        Finance.Setup(source => source.Read(2026, 9, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new FinanceExposure("001", 0.001m, 1) });
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PostAsync("/api/reports/monthly/2026/9", null)).StatusCode);
        await InDatabase(async db => { Assert.Empty(db.Reports); Assert.Empty(db.AuditLogs); });
    }

    [Fact]
    public async Task SourceOutageNeverFallsBackToLocalNumbersOrPersistsAFalseSuccess()
    {
        await SeedExposure(); Finance.SetupGet(source => source.Enabled).Returns(true);
        Finance.Setup(source => source.Read(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessRuleException(503, "finance.source_unavailable", "Finansijski izvor nije dostupan."));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await Client.PostAsync("/api/reports/monthly/2026/9", null)).StatusCode);
        await InDatabase(async db => { Assert.Empty(db.Reports); Assert.Empty(db.AuditLogs); Assert.Equal(1, (await db.ClientLimits.SingleAsync()).CurrentExposure); });
    }
}
