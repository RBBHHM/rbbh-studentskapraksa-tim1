using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using IntegrationTests.Tests.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;

namespace IntegrationTests.Tests;

public abstract class CodebookCsvFixture : LocalizationFixture
{
    protected const string Header = "VALUE_KEY,NAME,DESCRIPTION,IS_ACTIVE,FLAG,COUNT,AMOUNT,PRECISE,DAY,INSTANT,TEXT\r\n";
    protected const string Valid = "ITEM_1,\"Naziv, test\",\"Prvi red\r\nDrugi red\",true,0,-42,1.25,1.000001,2024-02-29,2026-12-31T13:45:00,Ž\r\n";

    protected async Task Define()
    {
        await InDatabase(async db =>
        {
            var definition = new CodeListDefinition { Name = "CSV_FIXTURE", CreatedBy = "fixture" };
            var entry = db.CodeListDefinitions.Add(definition);
            foreach (var (slot, name) in new[] { (2, "FLAG"), (3, "COUNT"), (6, "AMOUNT"), (8, "PRECISE"), (9, "DAY"), (11, "INSTANT"), (17, "TEXT") })
                entry.Property($"ColumnName{slot}").CurrentValue = name;
            await db.SaveChangesAsync();
        });
    }

    protected async Task<JsonElement> Import(string category, string data, bool dryRun)
    {
        using var body = new MultipartFormDataContent();
        body.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(data)), "file", "values.csv");
        using var response = await Client.PostAsync($"/api/code-lists/{category}/csv-import?dryRun={dryRun}", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

}

public sealed class CodebookCsvTests : CodebookCsvFixture
{
    [Fact]
    public async Task TemplateMatchesTypedColumnsAndAtomicImportPreservesNativeValues()
    {
        await Define();
        using var download = await Client.GetAsync("/api/code-lists/CSV_FIXTURE/csv-template");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        var template = await download.Content.ReadAsStringAsync();
        Assert.Contains("\"FLAG\",\"COUNT\",\"AMOUNT\",\"PRECISE\",\"DAY\",\"INSTANT\",\"TEXT\"", template);
        var preview = await Import("CSV_FIXTURE", template + Valid, true);
        Assert.True(preview.GetProperty("exampleSkipped").GetBoolean());
        Assert.Equal(1, preview.GetProperty("ready").GetInt32());
        await InDatabase(async db => Assert.Empty(await db.CodeLists.Where(row => row.Kategorija == "CSV_FIXTURE").ToListAsync()));

        var result = await Import("CSV_FIXTURE", template + Valid, false);
        Assert.Equal(1, result.GetProperty("imported").GetInt32());
        await InDatabase(async db =>
        {
            var row = await db.CodeLists.SingleAsync(row => row.Kategorija == "CSV_FIXTURE");
            Assert.Equal("Naziv, test", row.Naziv);
            Assert.Contains("Drugi red", row.Opis);
            var entry = db.Entry(row);
            Assert.False(entry.Property<bool?>("Slot2").CurrentValue);
            Assert.Equal(-42, entry.Property<int?>("Slot3").CurrentValue);
            Assert.Equal(1.25m, entry.Property<decimal?>("Slot6").CurrentValue);
            Assert.Equal(new DateTime(2024, 2, 29), entry.Property<DateTime?>("Slot9").CurrentValue);
            Assert.Equal("Ž", entry.Property<string>("Slot17").CurrentValue);
            Assert.Single(await db.AuditLogs.Where(log => log.Action == "IMPORT").ToListAsync());
        });
        var duplicate = await Import("CSV_FIXTURE", Header + Valid, false);
        Assert.Equal(1, duplicate.GetProperty("duplicates").GetInt32());
        Assert.Equal(0, duplicate.GetProperty("imported").GetInt32());
    }

    [Theory]
    [InlineData("NAME,NAME\nname,name", "codes.csv.header-duplicate")]
    [InlineData("UNKNOWN\nvalue", "codes.csv.header-extra")]
    [InlineData("VALUE_KEY,NAME\ncode,name", "codes.csv.header-missing")]
    [InlineData(Header + "ITEM_1,\"unterminated", "codes.csv.syntax")]
    [InlineData(Header + "ITEM_1,bare\"quote", "codes.csv.syntax")]
    [InlineData(Header + "ITEM_1,n", "codes.csv.cells")]
    [InlineData(Header + "ITEM_2,,Description,true,true,42,1.25,1.000001,2024-02-29,2026-12-31T13:45:00,text", "codes.csv.required")]
    [InlineData(Header + Valid + "ITEM_2,name,Description,true,true,42,1.234,1.000001,2024-02-29,2026-12-31T13:45:00,text", "codes.csv.invalid")]
    [InlineData(Header + Valid + "ITEM_2,name,Description,true,true,42,1.25,1.000001,2026-02-29,2026-12-31T13:45:00,text", "codes.csv.invalid")]
    [InlineData(Header + Valid + Valid, "codes.csv.file-duplicate")]
    public async Task InvalidCsvReportsTheProblemAndWritesNothing(string data, string expected)
    {
        await Define();
        var result = await Import("CSV_FIXTURE", data, false);
        Assert.Contains(result.GetProperty("errors").EnumerateArray(), issue => issue.GetProperty("key").GetString() == expected);
        Assert.Equal(0, result.GetProperty("imported").GetInt32());
        await InDatabase(async db => Assert.Empty(await db.CodeLists.Where(row => row.Kategorija == "CSV_FIXTURE").ToListAsync()));
    }

    [Fact]
    public async Task LanguagePacksSupportSemicolonsAndPublishTranslationsByKey()
    {
        await Seed();
        var csv = "IS_ACTIVE;LANGUAGE_CODE;RESOURCE_KEY;CONTEXT_INFO;LABEL_TEXT;HELP_TEXT;PLACEHOLDER_TEXT\n" +
                  "1;BS;example.csv;Codebooks;\"Šifra; naziv\";Pomoć;Primjer";
        var result = await Import("LANGUAGE_PACKS", csv, false);
        Assert.Equal(1, result.GetProperty("imported").GetInt32());
        var pack = await Pack("BS");
        Assert.Contains(pack.GetProperty("items").EnumerateArray(), row =>
            row.GetProperty("resourceKey").GetString() == "example.csv" && row.GetProperty("label").GetString() == "Šifra; naziv");
    }

}

public sealed class CodebookCsvSqlTests : CodebookCsvFixture
{
    protected override bool UseSql => true;

    [SqlServerFact]
    public async Task UpgradeRenamesPopulatedCodebooksWithoutReplacingDefinitionsOrExistingValues()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = "RPR_CODEBOOK_UPGRADE_" + Guid.NewGuid().ToString("N") };
        await using var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>()
            .UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20261126135008_ConsolidateCodebookLanguagePacks");
            await db.Database.ExecuteSqlRawAsync("""
                INSERT dbo.CODELIST_DEFINITIONS (NAME, IS_ACTIVE, IS_SYSTEM_REQUIRED, VERSION, CREATED_BY, CREATED_AT)
                VALUES ('VrstaLimita', 1, 0, 7, 'operator', SYSUTCDATETIME()),
                       ('CustomCodeBook', 1, 0, 7, 'operator', SYSUTCDATETIME());

                INSERT dbo.CODELIST_VALUES (DEFINITION_ID, CATEGORY, VALUE_KEY, NAME, IS_SYSTEM_REQUIRED, ID1, CREATED_BY, CREATED_AT)
                SELECT ID, NAME, 'MM', N'Operator label', 0, 0, 'operator', SYSUTCDATETIME()
                FROM dbo.CODELIST_DEFINITIONS WHERE NAME = 'VrstaLimita';
                """);
            var id = await db.CodeListDefinitions.Where(item => item.Name == "VrstaLimita").Select(item => item.Id).SingleAsync();
            await db.Database.MigrateAsync();
            Assert.Equal("VRSTE_LIMITA", (await db.CodeListDefinitions.FindAsync(id))!.Name);
            Assert.True(await db.CodeListDefinitions.AnyAsync(item => item.Name == "CUSTOM_CODE_BOOK"));
            var row = await db.CodeLists.IgnoreQueryFilters().SingleAsync(item => item.Kategorija == "VRSTE_LIMITA" && item.Kod == "MM");
            Assert.Equal("Operator label", row.Naziv);
            Assert.False(row.Aktivan);
            Assert.Equal(id, row.DefinitionId);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    [SqlServerFact]
    public async Task SqlServerStoresImportedValuesAndSeedsCanonicalLimitTypes()
    {
        await Define();
        Assert.Equal(1, (await Import("CSV_FIXTURE", Header + Valid, false)).GetProperty("imported").GetInt32());
        await InDatabase(async db =>
        {
            var row = await db.CodeLists.SingleAsync(item => item.Kategorija == "CSV_FIXTURE");
            Assert.Equal(1.000001m, db.Entry(row).Property<decimal?>("Slot8").CurrentValue);
            Assert.NotNull(row.DefinitionId);
            Assert.True(await db.CodeLists.AnyAsync(item => item.Kategorija == "VRSTE_LIMITA" && item.Kod == "MM"));
            Assert.False(await db.CodeLists.IgnoreQueryFilters().AnyAsync(item => item.Kategorija == "VrstaLimita"));
        });
        Assert.Contains((await Pack("DE")).GetProperty("items").EnumerateArray(),
            item => item.GetProperty("resourceKey").GetString() == "codes.csv.template");
    }
}
