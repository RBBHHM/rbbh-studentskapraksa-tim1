using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.Entities.RelatedPersons;

namespace IntegrationTests.Tests.LegalEntity;

public sealed class RegistryWorkbookHttpTests : AuthenticatedApiFixture
{
    private readonly Mock<IEmailService> _mail = new();
    protected override void ConfigureServices(IServiceCollection services)
    {
        RBBH.ConnectedParties.DL.Mapper.MapsterConfiguration.RegisterMappings(); services.AddSingleton(_mail.Object);
    }
    private async Task<byte[]> Export(bool legal) => await Client.GetByteArrayAsync(legal ? "/api/legal-entities/export" : "/api/related-persons/export");
    private async Task<JsonElement> Import(bool legal, byte[] bytes)
    {
        using var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent(bytes), "file", "fixture.xlsx");
        var response = await Client.PostAsync(legal ? "/api/legal-entities/import" : "/api/related-persons/import", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    [Fact]
    public async Task LegalV2RoundtripPreservesBusinessFieldsAndIgnoresForgedMetadataAndStatus()
    {
        var request = new { name = "Roundtrip company", isResident = false, fbaId = "12345", matbroj = "000123", maticniBroj = "000456",
            basisOfConnection = "VL", connectionDescription = "Real description", connectedWithBank = true, dateFrom = "2026-03-11", dateTo = "2026-12-05", isLargeExposure = true };
        Assert.Equal(HttpStatusCode.Created, (await Client.PostAsJsonAsync("/api/legal-entities", request)).StatusCode);
        using var original = new MemoryStream(await Export(true)); using var workbook = new XLWorkbook(original); var sheet = workbook.Worksheet(1);
        Assert.Equal(19, sheet.LastColumnUsed()!.ColumnNumber()); sheet.Cell(2, 13).Value = "Forged GCC name";
        sheet.Cell(2, 15).Value = "Verified"; sheet.Cell(2, 16).Value = "Forged creator"; sheet.Cell(2, 17).Value = new DateTime(2000, 1, 1);
        using var edited = new MemoryStream(); workbook.SaveAs(edited);
        var target = new RegistryWorkbookHttpTests(); await target.InitializeAsync();
        try
        {
            var result = await target.Import(true, edited.ToArray()); Assert.Equal(1, result.GetProperty("imported").GetInt32()); Assert.Equal(0, result.GetProperty("failed").GetInt32());
            await target.InDatabase(async db =>
            {
                var row = await db.LegalEntities.SingleAsync(); Assert.Equal("000123", row.Matbroj); Assert.Equal("000456", row.MaticniBroj);
                Assert.Equal(new DateTime(2026, 3, 11), row.DateFrom); Assert.Equal(new DateTime(2026, 12, 5), row.DateTo);
                Assert.True(row.IsLargeExposure); Assert.Null(row.GccName); Assert.Equal("Draft", row.Status);
                Assert.Equal("creator", row.CreatedBy); Assert.Equal("creator", row.CreatorSubject); Assert.NotEqual(2000, row.CreatedAt.Year);
                var audit = await db.AuditLogs.SingleAsync(); Assert.Equal("IMPORT", audit.Action); Assert.Equal("creator", audit.Username);
            });
        }
        finally { await target.DisposeAsync(); }
    }
    [Fact]
    public async Task PhysicalV2RoundtripResolvesChildBeforeParentAndPreservesDescriptionsWithoutHrMail()
    {
        var parent = await Client.PostAsJsonAsync("/api/related-persons", RegistryVerificationHttpTests.Physical());
        var id = (await parent.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Created, (await Client.PostAsJsonAsync("/api/related-persons", new
        { firstName = "Child", lastName = "Fixture", residency = 2, passportNumber = "CHILD01", fbaId = "12345", relationBasis = "VL", relationDescription = "Child relationship description",
            specialRelationBasis = "UZA_PORODICA", relatedToPersonId = id, dateFrom = "2026-01-01", dateTo = "2027-12-31" })).StatusCode);
        using var original = new MemoryStream(await Export(false)); using var workbook = new XLWorkbook(original); var sheet = workbook.Worksheet(1);
        Assert.Equal(25, sheet.LastColumnUsed()!.ColumnNumber());
        var child = sheet.Cell(2, 1).GetString() == "Child" ? 2 : 3; Assert.Equal("2801984175000", sheet.Cell(child, 18).GetString());
        // Order is intentionally reversed; import must resolve the graph, not rely on export sort.
        if (child == 3) for (var column = 1; column <= 25; column++) { var value = sheet.Cell(2, column).Value; sheet.Cell(2, column).Value = sheet.Cell(3, column).Value; sheet.Cell(3, column).Value = value; }
        using var edited = new MemoryStream(); workbook.SaveAs(edited);
        var target = new RegistryWorkbookHttpTests(); await target.InitializeAsync();
        try
        {
            var result = await target.Import(false, edited.ToArray()); Assert.Equal(2, result.GetProperty("imported").GetInt32()); Assert.Equal(0, result.GetProperty("failed").GetInt32());
            await target.InDatabase(async db =>
            {
                var rows = await db.RelatedPersons.ToListAsync(); var importedParent = rows.Single(row => row.FirstName == "Fixture"); var importedChild = rows.Single(row => row.FirstName == "Child");
                Assert.Equal(importedParent.Id, importedChild.RelatedToPersonId); Assert.Equal("Child relationship description", importedChild.RelationDescription);
                Assert.False(importedChild.IsIdentifiedStaff); Assert.True(importedChild.DeclarationNoFamilyMembers); Assert.All(rows, row => Assert.Equal(RelatedPersonStatus.Draft, row.Status));
            });
            target._mail.VerifyNoOtherCalls();
        }
        finally { await target.DisposeAsync(); }
        _mail.VerifyNoOtherCalls();
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CyclesAndInvalidParentsNeverProduceDanglingFamilyLinks(bool cycle)
    {
        var parent = await Client.PostAsJsonAsync("/api/related-persons", RegistryVerificationHttpTests.Physical());
        var id = (await parent.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await Client.PostAsJsonAsync("/api/related-persons", new { firstName = "Child", lastName = "Fixture", residency = 2, passportNumber = "CHILD01", fbaId = "12345",
            relationBasis = "VL", relationDescription = "Description", specialRelationBasis = "UZA_PORODICA", relatedToPersonId = id, dateFrom = "2026-01-01", dateTo = "2027-12-31" });
        using var source = new MemoryStream(await Export(false)); using var workbook = new XLWorkbook(source); var sheet = workbook.Worksheet(1);
        var parentRow = sheet.Cell(2, 1).GetString() == "Fixture" ? 2 : 3;
        if (cycle) { sheet.Cell(parentRow, 19).Value = "CHILD01"; sheet.Cell(parentRow, 20).Value = "12345"; }
        else sheet.Cell(parentRow, 1).Value = new string('X', 101);
        using var output = new MemoryStream(); workbook.SaveAs(output);
        var target = new RegistryWorkbookHttpTests(); await target.InitializeAsync();
        try
        {
            var result = await target.Import(false, output.ToArray()); Assert.Equal(0, result.GetProperty("imported").GetInt32()); Assert.Equal(2, result.GetProperty("failed").GetInt32());
            Assert.Equal(2, result.GetProperty("details").GetArrayLength());
            await target.InDatabase(async db => { Assert.Empty(await db.RelatedPersons.ToListAsync()); Assert.Empty(await db.AuditLogs.ToListAsync()); });
        }
        finally { await target.DisposeAsync(); }
    }

    [Fact]
    public async Task InvalidMixedRowReturnsStructuredDetailsAndCommitsValidRowsWithOneAudit()
    {
        Assert.Equal(HttpStatusCode.Created, (await Client.PostAsJsonAsync("/api/legal-entities", RegistryVerificationHttpTests.Legal())).StatusCode);
        using var original = new MemoryStream(await Export(true)); using var workbook = new XLWorkbook(original); var sheet = workbook.Worksheet(1);
        sheet.Row(2).CopyTo(sheet.Row(3)); sheet.Cell(3, 1).Value = new string('X', 101); sheet.Cell(3, 4).Value = "67890";
        using var edited = new MemoryStream(); workbook.SaveAs(edited);
        var target = new RegistryWorkbookHttpTests(); await target.InitializeAsync();
        try
        {
            var result = await target.Import(true, edited.ToArray()); Assert.Equal(1, result.GetProperty("imported").GetInt32()); Assert.Equal(1, result.GetProperty("failed").GetInt32());
            Assert.Equal(3, result.GetProperty("details")[0].GetProperty("row").GetInt32()); Assert.Single(result.GetProperty("errors").EnumerateArray());
            await target.InDatabase(async db => { Assert.Single(db.LegalEntities); Assert.Single(db.AuditLogs); });
        }
        finally { await target.DisposeAsync(); }
    }
}
