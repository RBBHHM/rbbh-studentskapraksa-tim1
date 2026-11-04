using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Tests.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace IntegrationTests.Tests;

public sealed class LocalizationSqlTests : LocalizationFixture
{
    protected override bool UseSql => true;

    [SqlServerFact]
    public async Task MigratedCatalogHasThreeLanguagesAndRuntimeVersionsInvalidateAcrossScopes()
    {
        var languages = await Client.GetFromJsonAsync<JsonElement>("/api/localization/languages");
        Assert.Equal(new[] { "BS", "DE", "EN" }, languages.GetProperty("items").EnumerateArray().Select(row => row.GetProperty("code").GetString()).Order());
        var initial = await Pack("DE");
        Assert.Contains(initial.GetProperty("items").EnumerateArray(), row => row.GetProperty("resourceKey").GetString() == "errors.request.invalid.title" && row.GetProperty("label").GetString() == "Die Anfrage ist ungültig.");
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PutAsJsonAsync("/api/localization/resources/BS/new.runtime.label", new { label = "Runtime" })).StatusCode);
        var changed = await Pack("DE"); Assert.NotEqual(initial.GetProperty("version").GetString(), changed.GetProperty("version").GetString());
        Assert.Contains(changed.GetProperty("items").EnumerateArray(), row => row.GetProperty("resourceKey").GetString() == "new.runtime.label" && row.GetProperty("label").GetString() == "Runtime");
    }
    [SqlServerFact]
    public async Task ResourceAndVersionRollbackWhenAuditFails()
    {
        var before = await Pack("BS");
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FixtureRejectResourceAudit ON AuditLogs AFTER INSERT AS BEGIN THROW 51000, 'fixture failure', 1; END"));
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.PutAsJsonAsync("/api/localization/resources/BS/rollback.resource", new { label = "No" })).StatusCode);
        var after = await Pack("BS"); Assert.Equal(before.GetProperty("version").GetString(), after.GetProperty("version").GetString());
        await InDatabase(async db => { Assert.False(await db.CodeLists.AnyAsync(row => row.ResourceKey == "rollback.resource")); Assert.Empty(db.AuditLogs); });
    }
    [SqlServerFact]
    public async Task BatchedCodebookImportRollsBackEveryValueIfAuditCannotBePersisted()
    {
        byte[] bytes;
        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Values"); sheet.Cell(1, 1).Value = "Value";
            sheet.Cell(2, 1).Value = "One"; sheet.Cell(3, 1).Value = "Two";
            using var stream = new MemoryStream(); workbook.SaveAs(stream); bytes = stream.ToArray();
        }
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FixtureRejectImportAudit ON AuditLogs AFTER INSERT AS BEGIN THROW 51000, 'fixture failure', 1; END"));
        using var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent(bytes), "file", "values.xlsx");
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.PostAsync("/api/code-lists/ImportRollback/import?dryRun=false", form)).StatusCode);
        await InDatabase(async db => { Assert.False(await db.CodeLists.AnyAsync(row => row.Kategorija == "ImportRollback")); Assert.False(await db.CodeListDefinitions.AnyAsync(row => row.Name == "ImportRollback")); Assert.Empty(db.AuditLogs); });
    }

    [SqlServerFact]
    public async Task ConcurrentNormalizedCodeCreationAllowsOneValueAndOneAudit()
    {
        var body = new { kategorija = "Race", kod = "VALUE", naziv = "Fixture" };
        var responses = await Task.WhenAll(Client.PostAsJsonAsync("/api/code-lists", body), Client.PostAsJsonAsync("/api/code-lists", body));
        Assert.Single(responses, row => row.StatusCode == HttpStatusCode.Created);
        Assert.All(responses.Where(row => row.StatusCode != HttpStatusCode.Created), row => Assert.Contains(row.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Conflict }));
        await InDatabase(async db => { Assert.Single(await db.CodeLists.Where(row => row.Kategorija == "Race").ToListAsync()); Assert.Single(await db.AuditLogs.ToListAsync()); });
    }
    [SqlServerFact]
    public async Task PopulatedUpgradeRejectsDuplicateKeysWithoutDeletingLegacyValues()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = "RBBH_CODEBOOK_PREFLIGHT_" + Guid.NewGuid().ToString("N") };
        await using var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync(db.Database.GetMigrations().Single(row => row.EndsWith("_EnforceNormalizedApplicationUsers")));
            await db.Database.ExecuteSqlRawAsync("INSERT CodeLists (Kategorija,Kod,Naziv,Aktivan,KreiranDatum,KreiraoKorisnik) VALUES ('Category','CODE',N'First',1,SYSUTCDATETIME(),'fixture'),('Category',' code ',N'Legacy inactive',0,SYSUTCDATETIME(),'fixture')");
            var error = await Assert.ThrowsAsync<SqlException>(() => db.Database.MigrateAsync()); Assert.Equal(51004, error.Number);
            await using var sql = new SqlConnection(connection.ConnectionString); await sql.OpenAsync();
            await using var count = sql.CreateCommand(); count.CommandText = "SELECT COUNT(*) FROM CodeLists";
            Assert.Equal(2, Convert.ToInt32(await count.ExecuteScalarAsync()));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
