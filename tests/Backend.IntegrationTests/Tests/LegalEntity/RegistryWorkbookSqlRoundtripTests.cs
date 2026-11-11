using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using IntegrationTests.Tests.Users;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Tests.LegalEntity;

/// <summary>
/// Full export/import through real HTTP controllers and two separate disposable
/// migrated SQL databases. Does not modify any shared or production data.
/// The existing workbook tests cover legacy headers, mixed rows and links.
/// </summary>
public sealed class RegistryWorkbookSqlRoundtripTests : AuthenticatedApiFixture
{
    protected override bool UseSql => true;

    [SqlServerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExportedRegistryWorkbookImportsIntoIndependentSqlDatabaseWithOneAudit(bool legal)
    {
        var route = legal ? "/api/legal-entities" : "/api/related-persons";
        var payload = legal ? RegistryVerificationHttpTests.Legal() : RegistryVerificationHttpTests.Physical();
        var response = await Client.PostAsJsonAsync(route, payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var bytes = await Client.GetByteArrayAsync(route + "/export");
        using (var stream = new MemoryStream(bytes))
        using (var workbook = new ClosedXML.Excel.XLWorkbook(stream))
        {
            var worksheet = workbook.Worksheet(1);
            Assert.Equal(legal ? 19 : 26, worksheet.LastColumnUsed()!.ColumnNumber());
            Assert.Equal(2, worksheet.LastRowUsed()!.RowNumber());
        }

        var target = new RegistryWorkbookSqlRoundtripTests();
        await target.InitializeAsync();
        try
        {
            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(bytes), "file", "registry-roundtrip.xlsx");
            var imported = await target.Client.PostAsync(route + "/import", form);
            Assert.Equal(HttpStatusCode.OK, imported.StatusCode);
            var result = await imported.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, result.GetProperty("imported").GetInt32());
            Assert.Equal(0, result.GetProperty("failed").GetInt32());

            await target.InDatabase(async db =>
            {
                if (legal)
                {
                    var entity = Assert.Single(await db.LegalEntities.ToListAsync());
                    Assert.Equal("Fixture company", entity.Name);
                    Assert.Equal("Draft", entity.Status);
                    Assert.Equal("creator", entity.CreatedBy);
                }
                else
                {
                    var entity = Assert.Single(await db.RelatedPersons.ToListAsync());
                    Assert.Equal("Fixture", entity.FirstName);
                    Assert.Equal("Person", entity.LastName);
                    Assert.Equal("2801984175000", entity.JMBG);
                    Assert.Equal("creator", entity.CreatedBy);
                }
                var audit = Assert.Single(await db.AuditLogs.ToListAsync());
                Assert.Equal("IMPORT", audit.Action);
            });
        }
        finally { await target.DisposeAsync(); }
    }
}
