using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Tests.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions;

namespace IntegrationTests.Tests;

public sealed class CapitalHistorySqlTests : CapitalHistoryFixture
{
    protected override bool UseSql => true;
    [SqlServerFact]
    public async Task ConcurrentCreatesLeaveExactlyOneActivePeriod()
    {
        await Task.WhenAll(Save(DateTime.UtcNow.Date.AddMonths(1), 100), Save(DateTime.UtcNow.Date.AddMonths(1), 200));
        decimal expected = 0;
        await InDatabase(async db =>
        {
            var rows = await db.Capitals.OrderBy(row => row.Id).ToListAsync(); Assert.Equal(2, rows.Count);
            Assert.True(rows[0].Id < rows[1].Id); expected = rows[1].RegulatoryCapital;
            Assert.Equal(2, await db.AuditLogs.CountAsync()); Assert.Single(rows, row => row.IsActive);
        });
        var asOf = await Client.GetFromJsonAsync<JsonElement>("/api/capital"); Assert.Equal(expected, asOf.GetProperty("regulatoryCapital").GetDecimal());
    }
    [SqlServerFact]
    public async Task AuditFailureRollsBackRevisionAndDoesNotChangeLegacyCapital()
    {
        await InDatabase(async db => { db.Capitals.Add(new() { CapitalDate = new(2026, 1, 1), RegulatoryCapital = 100, CreatedBy = "original" }); await db.SaveChangesAsync(); });
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FixtureCapitalAuditFailure ON AUDIT_LOGS AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE TABLE_NAME = 'Capital') THROW 51000, 'fixture failure', 1; END"));
        var response = await Client.PostAsJsonAsync("/api/capital", new { regulatoryCapital = 200, coreCapital = 100, supplementaryCapital = 50, validTill = DateTime.UtcNow.Date.AddMonths(1) });
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await InDatabase(async db => { Assert.Empty(db.AuditLogs); Assert.Equal(100, (await db.Capitals.SingleAsync()).RegulatoryCapital); });
    }
    [SqlServerFact]
    public async Task PopulatedUpgradeRetainsOnlyKnownLegacyDateAndDoesNotBackfillHistory()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = "RBBH_CAPITAL_UPGRADE_" + Guid.NewGuid().ToString("N") };
        await using var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20261126075426_UseNumericRegistryKeys");
            await db.Database.ExecuteSqlRawAsync("""
                INSERT dbo.Capitals (RegulatoryCapital,CoreCapital,SupplementaryCapital,CapitalDate,CreatedBy,CreatedAt,IsActive)
                VALUES (100,50,25,'20260101','original',SYSUTCDATETIME(),1);
                INSERT dbo.CapitalRevisions (RegulatoryCapital,CoreCapital,SupplementaryCapital,EffectiveDate,RecordedBy,RecordedAt)
                VALUES (200,100,50,'20260201','operator',SYSUTCDATETIME());
                DECLARE @entity bigint = NEXT VALUE FOR dbo.LegalEntitiesIdSequence;
                INSERT dbo.LegalEntities (Id,IsResident,Name,BasisOfConnection,Status,CreatedBy,CreatedAt,IsActive)
                VALUES (@entity,0,'Fixture','VL','Draft','original',SYSUTCDATETIME(),1);
                INSERT dbo.ClientLimits (Id,LegalEntityId,RegulatoryCapital,CoreCapital,SupplementaryCapital,CapitalDate,
                    ExposureLimit,CurrentExposure,Currency,IsLimitBreached,CreatedBy,CreatedAt,IsActive)
                VALUES (NEXT VALUE FOR dbo.ClientLimitsIdSequence,@entity,200,100,50,'20260201',150,160,'EUR',1,
                    'operator','20260202',1);
                INSERT dbo.AuditLogs (Action,Username,TableName,RecordId,Timestamp)
                VALUES ('VERIFY','operator','LegalEntity',CONVERT(nvarchar(20), @entity),SYSUTCDATETIME());
                """);

            await db.Database.MigrateAsync();
            Assert.Equal(2, await db.Capitals.CountAsync());
            var labels = await db.Database.SqlQueryRaw<string>("""
                SELECT RESOURCE_LABEL AS [Value] FROM dbo.CODE_LIST_LOCALIZATION
                WHERE CATEGORY = N'LanguagePacks' AND RESOURCE_KEY = N'capital.create' ORDER BY LANGUAGE_CODE
                """).ToListAsync();
            Assert.Equal(new[] { "Novi kapital", "Neues Kapital", "New capital" }, labels);
            var active = await db.Capitals.SingleAsync(row => row.IsActive);
            Assert.Equal(200, active.RegulatoryCapital);
            var limit = await db.Limiti.SingleAsync();
            Assert.Equal(active.Id, limit.CapitalId);
            Assert.Equal(150, limit.OdobreniLimit);
            Assert.Equal(160, limit.MaksimalnaOcekivanaUtilizacija);
            Assert.Contains("EUR", limit.Komentar);
            Assert.Equal(limit.LegalEntityId.ToString(), (await db.AuditLogs.SingleAsync()).RecordId);

            var service = new CapitalService(db);
            Assert.Null((await service.AsOf(new(2025, 12, 31), default)).Value);
            Assert.Equal(100, (await service.AsOf(new(2026, 1, 31), default)).Value!.RegulatoryCapital);
            Assert.Equal(200, (await service.AsOf(new(2026, 2, 1), default)).Value!.RegulatoryCapital);

            Task<List<string>> Columns(string table) => db.Database.SqlQuery<string>($"""
                SELECT name AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID({table}) ORDER BY column_id
                """).ToListAsync();
            Assert.Equal(new[] { "ID", "KEYCLOAK_ID", "USERNAME", "FIRST_NAME", "LAST_NAME", "EMAIL", "IS_ACTIVE",
                "LOGIN_CONFIRMED", "CREATED_AT", "CREATED_BY", "MODIFIED_AT", "MODIFIED_BY" }, await Columns("dbo.APP_USERS"));
            Assert.Equal(new[] { "ID", "ACTION", "USER", "TABLE_NAME", "OLD_VALUES", "NEW_VALUES", "TIMESTAMP" },
                await Columns("dbo.AUDIT_LOGS"));
            Assert.Equal(new[] { "ID", "IS_ACTIVE", "VALID_TILL", "REGULATORY_CAPITAL", "CORE_CAPITAL", "SUPPLEMENTARY_CAPITAL",
                "CREATED_BY", "CREATED_AT", "MODIFIED_BY", "MODIFIED_AT", "VALID_FROM" }, await Columns("dbo.CAPITALS"));
            Assert.Equal(new[] { "ID", "LIMIT_TYPE", "APPROVED_LIMIT", "CAPITAL_ID", "LEGAL_ENTITY_ID",
                "EXPECTED_UTILIZATION_DATE", "MAXIMUM_EXPECTED_UTILIZATION", "COMMENT", "CREATED_BY", "CREATED_AT",
                "MODIFIED_BY", "MODIFIED_AT" }, await Columns("dbo.LIMITS"));
            Assert.Equal(0, await db.Database.SqlQueryRaw<int>("""
                SELECT COUNT(*) AS [Value]
                FROM sys.indexes AS indexes
                INNER JOIN sys.index_columns AS keys ON keys.object_id = indexes.object_id AND keys.index_id = indexes.index_id
                INNER JOIN sys.columns AS columns ON columns.object_id = keys.object_id AND columns.column_id = keys.column_id
                WHERE indexes.is_primary_key = 1 AND TYPE_NAME(columns.system_type_id) = N'uniqueidentifier'
                """).SingleAsync());
            Assert.Equal(0, await db.Database.SqlQueryRaw<int>("""
                SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name IN (N'CapitalRevisions', N'ClientLimits')
                """).SingleAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
