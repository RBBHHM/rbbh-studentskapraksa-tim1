using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;

namespace IntegrationTests.Tests;

public abstract class LimitImportFixture : AuthenticatedApiFixture
{
    protected override void ConfigureServices(IServiceCollection services) => services.AddScoped<ILimitService, LimitService>();
    protected Guid Party;
    protected async Task Seed()
    {
        await InDatabase(async db =>
        {
            var party = new RBBH.ConnectedParties.DL.Entities.LegalEntity.LegalEntity { Name = "Fixture company", FbaId = "12345", Matbroj = "00123", CreatedBy = "fixture" };
            db.LegalEntities.Add(party); Party = party.Id;
            db.CodeLists.Add(new() { Kategorija = "VrstaLimita", Kod = "TYPE", Naziv = "Fixture type", KreiraoKorisnik = "fixture" }); await db.SaveChangesAsync();
        });
    }
    protected object Body(decimal approved = 100.50m, string comment = "Original") => new { legalEntityId = Party, tipLimita = "TYPE", odobreniLimit = approved,
        maksimalnaOcekivanaUtilizacija = 25m, rokOcekivaneUtilizacije = "2026-12-15", komentar = comment };
    protected async Task<int> Create()
    {
        var response = await Client.PostAsJsonAsync("/api/limiti", Body()); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }
    protected async Task<byte[]> File(decimal? changed = null)
    {
        using var source = new MemoryStream(await Client.GetByteArrayAsync("/api/limiti/export")); using var workbook = new XLWorkbook(source);
        if (changed.HasValue) workbook.Worksheet(1).Cell(2, 8).Value = changed.Value;
        using var output = new MemoryStream(); workbook.SaveAs(output); return output.ToArray();
    }
    protected async Task<HttpResponseMessage> Import(byte[] bytes, HttpClient? client = null)
    {
        using var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent(bytes), "file", "fixture.xlsx"); return await (client ?? Client).PostAsync("/api/limiti/import", form);
    }
}

public sealed class LimitImportHttpTests : LimitImportFixture
{
    [Fact]
    public async Task CreationAuditIdentifiesGeneratedRowAndRoundtripRebindsBusinessIdentityWithoutTrustingMetadata()
    {
        await Seed(); var id = await Create();
        await InDatabase(async db => { var audit = await db.AuditLogs.SingleAsync(); Assert.Equal(id.ToString(), audit.RecordId); Assert.Equal("creator", audit.UserId); });
        using var original = new MemoryStream(await File()); using var workbook = new XLWorkbook(original); var sheet = workbook.Worksheet(1);
        Assert.Equal(18, sheet.LastColumnUsed()!.ColumnNumber()); sheet.Cell(2, 15).Value = "Forged actor"; sheet.Cell(2, 12).Value = 999999;
        using var edited = new MemoryStream(); workbook.SaveAs(edited);
        var target = new LimitImportHttpTests(); await target.InitializeAsync();
        try
        {
            await target.Seed(); var response = await target.Import(edited.ToArray()); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(1, result.GetProperty("imported").GetInt32()); Assert.Equal(0, result.GetProperty("failed").GetInt32());
            await target.InDatabase(async db =>
            {
                var row = await db.Limiti.SingleAsync(); Assert.Equal(target.Party, row.LegalEntityId); Assert.Equal(100.50m, row.OdobreniLimit); Assert.Equal(new DateTime(2026, 12, 15), row.RokOcekivaneUtilizacije);
                Assert.Equal("creator", row.CreatedBy); Assert.NotNull(row.ImportFingerprint); Assert.Empty(db.Capitals); Assert.Empty(db.CapitalRevisions);
                var audit = await db.AuditLogs.SingleAsync(); using var data = JsonDocument.Parse(audit.NewValues!); Assert.Equal(row.Id, data.RootElement.GetProperty("ids")[0].GetInt32());
            });
        }
        finally { await target.DisposeAsync(); }
    }
    [Fact]
    public async Task MixedRowsAndRepeatedImportReportExactFailuresWithoutDuplicateWrites()
    {
        await Seed(); await Create(); using var original = new MemoryStream(await File(200)); using var workbook = new XLWorkbook(original); var sheet = workbook.Worksheet(1);
        sheet.Row(2).CopyTo(sheet.Row(3)); sheet.Cell(3, 7).Value = "UNKNOWN"; sheet.Row(2).CopyTo(sheet.Row(4));
        using var edited = new MemoryStream(); workbook.SaveAs(edited);
        var first = await Import(edited.ToArray()); var data = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, data.GetProperty("imported").GetInt32()); Assert.Equal(2, data.GetProperty("failed").GetInt32()); Assert.Equal(2, data.GetProperty("details").GetArrayLength());
        var repeat = await Import(edited.ToArray()); data = await repeat.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(0, data.GetProperty("imported").GetInt32()); Assert.Equal(3, data.GetProperty("failed").GetInt32());
        await InDatabase(async db => { Assert.Equal(2, await db.Limiti.CountAsync()); Assert.Equal(2, await db.AuditLogs.CountAsync()); });
    }
    [Fact]
    public async Task PriorFourteenColumnExportIsAcceptedAsLegacyImport()
    {
        await Seed(); await Create(); using var original = new MemoryStream(await File(200)); using var workbook = new XLWorkbook(original);
        workbook.Worksheet(1).Columns(15, 18).Delete(); using var edited = new MemoryStream(); workbook.SaveAs(edited);
        var response = await Import(edited.ToArray()); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("imported").GetInt32());
    }

    [Theory]
    [InlineData(-1, "Valid")]
    [InlineData(1.111, "Valid")]
    public async Task InvalidAmountsNeverPersist(decimal amount, string comment)
    {
        await Seed(); Assert.Equal(HttpStatusCode.BadRequest, (await Client.PostAsJsonAsync("/api/limiti", Body(amount, comment))).StatusCode);
        await InDatabase(async db => { Assert.Empty(db.Limiti); Assert.Empty(db.AuditLogs); });
    }
    [Fact]
    public async Task ImportRetainsExistingAuthorizationAndPeriodLock()
    {
        await Seed(); await Create(); var bytes = await File(200);
        using var guest = NewClient("guest"); using var anonymous = NewClient("anonymous");
        Assert.Equal(HttpStatusCode.Forbidden, (await Import(bytes, guest)).StatusCode); Assert.Equal(HttpStatusCode.Unauthorized, (await Import(bytes, anonymous)).StatusCode);
        await InDatabase(async db => { (await db.PeriodLocks.SingleAsync()).IsLocked = true; await db.SaveChangesAsync(); });
        Assert.Equal((HttpStatusCode)423, (await Import(bytes)).StatusCode);
        await InDatabase(async db => { Assert.Single(db.Limiti); Assert.Single(db.AuditLogs); });
    }
    [Fact]
    public async Task ContradictoryPartyIdentifiersAndOverlongCommentsAreRejectedPerRow()
    {
        await Seed(); await Create(); using var original = new MemoryStream(await File(200)); using var workbook = new XLWorkbook(original); var sheet = workbook.Worksheet(1);
        sheet.Cell(2, 6).Value = "UNKNOWN"; sheet.Row(2).CopyTo(sheet.Row(3)); sheet.Cell(3, 6).Value = "00123"; sheet.Cell(3, 11).Value = new string('X', 1001);
        using var edited = new MemoryStream(); workbook.SaveAs(edited); var response = await Import(edited.ToArray()); var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, result.GetProperty("imported").GetInt32()); Assert.Equal(2, result.GetProperty("failed").GetInt32());
        await InDatabase(async db => { Assert.Single(db.Limiti); Assert.Single(db.AuditLogs); });
    }
}
