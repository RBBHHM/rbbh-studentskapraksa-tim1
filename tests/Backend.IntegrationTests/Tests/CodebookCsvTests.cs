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
    public async Task CreateReturnsCanonicalNameAndDuplicateLeavesSingleDefinition()
    {
        var request = new { name = "testni sifrarnik", description = "Testni šifrarnik" };
        using var created = await Client.PostAsJsonAsync("/api/code-lists/categories", request);
        Assert.True(created.StatusCode == HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var result = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("TESTNI_SIFRARNIK", result.GetProperty("name").GetString());
        using var columns = await Client.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, columns.StatusCode);

        using var duplicate = await Client.PostAsJsonAsync("/api/code-lists/categories", new { name = "TESTNI_SIFRARNIK" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        await InDatabase(async db =>
        {
            Assert.Single(await db.CodeListDefinitions.Where(row => row.Name == "TESTNI_SIFRARNIK").ToListAsync());
            Assert.Single(await db.AuditLogs.Where(row => row.Action == "CODELIST_ADD").ToListAsync());
        });
    }

    [SqlServerFact]
    public async Task UpgradeAcceptsAlreadyCanonicalLimitCodebook()
    {
        await UpgradeDatabase(async db =>
        {
            await db.Database.ExecuteSqlRawAsync("""
                INSERT dbo.CODELIST_DEFINITIONS (NAME, IS_ACTIVE, IS_SYSTEM_REQUIRED, VERSION, CREATED_BY, CREATED_AT)
                VALUES ('VRSTE_LIMITA', 1, 0, 7, 'operator', SYSUTCDATETIME());
                """);
            var id = await db.CodeListDefinitions.Where(item => item.Name == "VRSTE_LIMITA").Select(item => item.Id).SingleAsync();

            await db.Database.MigrateAsync();

            Assert.Equal("VRSTE_LIMITA", (await db.CodeListDefinitions.FindAsync(id))!.Name);
            Assert.True(await db.CodeLists.AnyAsync(item => item.Kategorija == "VRSTE_LIMITA" && item.Kod == "MM"));
        });
    }

    [SqlServerFact]
    public async Task UpgradePreservesRealCollisionsAndIncludesLegacyLimitCodes()
    {
        await UpgradeDatabase(async db =>
        {
            await db.Database.ExecuteSqlRawAsync("""
                INSERT dbo.CODELIST_DEFINITIONS (NAME, IS_ACTIVE, IS_SYSTEM_REQUIRED, VERSION, CREATED_BY, CREATED_AT)
                VALUES ('VRSTE_LIMITA', 1, 0, 7, 'operator', SYSUTCDATETIME()),
                       ('VrstaLimita', 1, 0, 7, 'operator', SYSUTCDATETIME()),
                       ('VRSTE_LIMITA_2', 1, 0, 7, 'operator', SYSUTCDATETIME()),
                       ('CUSTOM_CODE_BOOK', 1, 0, 7, 'operator', SYSUTCDATETIME()),
                       ('CustomCodeBook', 1, 0, 7, 'operator', SYSUTCDATETIME());

                INSERT dbo.CODELIST_VALUES (DEFINITION_ID, CATEGORY, VALUE_KEY, NAME, IS_SYSTEM_REQUIRED, ID1, CREATED_BY, CREATED_AT)
                SELECT definition.ID, definition.NAME, seed.Code, seed.Label, 0, 1, 'operator', SYSUTCDATETIME()
                FROM (VALUES ('VRSTE_LIMITA', 'MM', N'Canonical label'), ('VrstaLimita', 'MM', N'Legacy label'),
                             ('VrstaLimita', 'LEGACY_CODE', N'Custom limit'), ('CustomCodeBook', 'OTHER_CODE', N'Other value'))
                     AS seed(Book, Code, Label)
                JOIN dbo.CODELIST_DEFINITIONS AS definition ON definition.NAME = seed.Book COLLATE Latin1_General_100_CI_AS;
                """);
            var originals = await db.CodeLists.AsNoTracking().Where(row => row.Kod == "MM" || row.Kod == "LEGACY_CODE" || row.Kod == "OTHER_CODE")
                .ToDictionaryAsync(row => row.Naziv, row => row.ID);
            var originalIds = originals.Values.ToArray();
            var canonicalId = await db.CodeListDefinitions.Where(row => row.Name == "VRSTE_LIMITA").Select(row => row.Id).SingleAsync();

            await db.Database.MigrateAsync();

            var rows = await db.CodeLists.AsNoTracking().Where(row => originalIds.Contains(row.ID)).ToDictionaryAsync(row => row.Naziv);
            Assert.Equal(originals.Count, rows.Count);
            Assert.Equal("VRSTE_LIMITA", rows["Canonical label"].Kategorija);
            Assert.Equal("VRSTE_LIMITA_3", rows["Legacy label"].Kategorija);
            Assert.True(await db.CodeListDefinitions.AnyAsync(row => row.Name == "VRSTE_LIMITA_2"));
            Assert.Equal("VRSTE_LIMITA", rows["Custom limit"].Kategorija);
            Assert.Equal(canonicalId, rows["Custom limit"].DefinitionId);
            Assert.Equal("CUSTOM_CODE_BOOK_2", rows["Other value"].Kategorija);
            Assert.True(await db.CodeListDefinitions.AnyAsync(row => row.Name == "CUSTOM_CODE_BOOK"));
        });
    }

    [SqlServerFact]
    public async Task UpgradeRenamesPopulatedCodebooksWithoutReplacingDefinitionsOrExistingValues()
    {
        await UpgradeDatabase(async db =>
        {
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
        });
    }

    private static async Task UpgradeDatabase(Func<ConnectedPartiesDbContext, Task> test)
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = "RPR_CODEBOOK_UPGRADE_" + Guid.NewGuid().ToString("N") };
        await using var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>()
            .UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20261126135008_ConsolidateCodebookLanguagePacks");
            await test(db);
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
