using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Infrastructure;
using IntegrationTests.Tests.Users;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Tests.LegalEntity;

public sealed class RegistryWriteSqlTests : AuthenticatedApiFixture
{
    protected override bool UseSql => true;
    private async Task RejectAudit(string action)
    {
        var sql = action switch
        {
            "INSERT" => "CREATE TRIGGER FixtureRegistryAuditFailure ON AuditLogs AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action = 'INSERT') THROW 51000, 'fixture audit failure', 1; END",
            "UPDATE" => "CREATE TRIGGER FixtureRegistryAuditFailure ON AuditLogs AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action = 'UPDATE') THROW 51000, 'fixture audit failure', 1; END",
            "DELETE" => "CREATE TRIGGER FixtureRegistryAuditFailure ON AuditLogs AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action = 'DELETE') THROW 51000, 'fixture audit failure', 1; END",
            "IMPORT" => "CREATE TRIGGER FixtureRegistryAuditFailure ON AuditLogs AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action = 'IMPORT') THROW 51000, 'fixture audit failure', 1; END",
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
        await InDatabase(db => db.Database.ExecuteSqlRawAsync(sql));
    }
    private async Task<Guid> Create(bool legal)
    {
        var response = await Client.PostAsJsonAsync(legal ? "/api/legal-entities" : "/api/related-persons", legal ? RegistryVerificationHttpTests.Legal() : RegistryVerificationHttpTests.Physical());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode); return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    [SqlServerFact]
    public async Task FailedCreationAuditRollsBackBothRegistries()
    {
        await RejectAudit("INSERT");
        foreach (var legal in new[] { true, false })
            Assert.Equal(HttpStatusCode.InternalServerError, (await Client.PostAsJsonAsync(legal ? "/api/legal-entities" : "/api/related-persons", legal ? RegistryVerificationHttpTests.Legal() : RegistryVerificationHttpTests.Physical())).StatusCode);
        await InDatabase(async db => { Assert.Empty(db.LegalEntities); Assert.Empty(db.RelatedPersons); Assert.Empty(await db.AuditLogs.ToListAsync()); });
    }
    [SqlServerFact]
    public async Task FailedUpdateAuditRetainsOriginalDataAndMetadata()
    {
        var legalId = await Create(true); var physicalId = await Create(false); await RejectAudit("UPDATE");
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.PutAsJsonAsync($"/api/legal-entities/{legalId}", RegistryVerificationHttpTests.Legal())).StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.PutAsJsonAsync($"/api/related-persons/{physicalId}", RegistryVerificationHttpTests.Physical())).StatusCode);
        await InDatabase(async db => { Assert.Null((await db.LegalEntities.SingleAsync()).ModifiedAt); Assert.Null((await db.RelatedPersons.SingleAsync()).ModifiedAt); Assert.Equal(2, await db.AuditLogs.CountAsync()); });
    }
    [SqlServerFact]
    public async Task FailedDeactivationAuditLeavesBothRecordsActive()
    {
        var legalId = await Create(true); var physicalId = await Create(false); await RejectAudit("DELETE");
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.DeleteAsync($"/api/legal-entities/{legalId}")).StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.DeleteAsync($"/api/related-persons/{physicalId}")).StatusCode);
        await InDatabase(async db => { Assert.True((await db.LegalEntities.SingleAsync()).IsActive); Assert.True((await db.RelatedPersons.SingleAsync()).IsActive); Assert.Equal(2, await db.AuditLogs.CountAsync()); });
    }
    [SqlServerFact]
    public async Task FailedImportAuditRollsBackEveryValidRow()
    {
        await Create(true);
        using var source = new MemoryStream(await Client.GetByteArrayAsync("/api/legal-entities/export")); using var workbook = new ClosedXML.Excel.XLWorkbook(source);
        var sheet = workbook.Worksheet(1); sheet.Cell(2, 4).Value = "67890"; sheet.Row(2).CopyTo(sheet.Row(3)); sheet.Cell(3, 4).Value = "67891";
        using var stream = new MemoryStream(); workbook.SaveAs(stream); await RejectAudit("IMPORT");
        using var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent(stream.ToArray()), "file", "fixture.xlsx");
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.PostAsync("/api/legal-entities/import", form)).StatusCode);
        await InDatabase(async db => { Assert.Single(db.LegalEntities); Assert.Single(db.AuditLogs); Assert.False(await db.LegalEntities.AnyAsync(row => row.FbaId == "67890" || row.FbaId == "67891")); });
    }
    [SqlServerFact]
    public async Task ConcurrentDuplicateLegalCreationCommitsOneRecordAndOneAudit()
    {
        var responses = await Task.WhenAll(Client.PostAsJsonAsync("/api/legal-entities", RegistryVerificationHttpTests.Legal()), Client.PostAsJsonAsync("/api/legal-entities", RegistryVerificationHttpTests.Legal()));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.All(responses.Where(response => response.StatusCode != HttpStatusCode.Created), response => Assert.Contains(response.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Conflict }));
        await InDatabase(async db => { Assert.Single(db.LegalEntities); Assert.Single(db.AuditLogs); });
    }
}
