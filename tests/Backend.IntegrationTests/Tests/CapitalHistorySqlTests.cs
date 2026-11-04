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
    public async Task ConcurrentCorrectionsBothSurviveAndDatabaseRevisionOrderDefinesLatest()
    {
        await Task.WhenAll(Save(new(2026, 1, 1), 100), Save(new(2026, 1, 1), 200));
        decimal expected = 0;
        await InDatabase(async db =>
        {
            var rows = await db.CapitalRevisions.OrderBy(row => row.Revision).ToListAsync(); Assert.Equal(2, rows.Count);
            Assert.True(rows[0].Revision < rows[1].Revision); expected = rows[1].RegulatoryCapital;
            Assert.Equal(2, await db.AuditLogs.CountAsync()); Assert.All(rows, row => Assert.Equal("creator", row.ActorSubject));
        });
        var asOf = await Client.GetFromJsonAsync<JsonElement>("/api/capital/as-of?date=2026-02-01"); Assert.Equal(expected, asOf.GetProperty("regulatoryCapital").GetDecimal());
    }
    [SqlServerFact]
    public async Task AuditFailureRollsBackRevisionAndDoesNotChangeLegacyCapital()
    {
        await InDatabase(async db => { db.Capitals.Add(new() { CapitalDate = new(2026, 1, 1), RegulatoryCapital = 100, CreatedBy = "original" }); await db.SaveChangesAsync(); });
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FixtureCapitalAuditFailure ON AuditLogs AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE TableName = 'Capital') THROW 51000, 'fixture failure', 1; END"));
        var response = await Client.PutAsJsonAsync("/api/capital", new { regulatoryCapital = 200, coreCapital = 100, supplementaryCapital = 50, capitalDate = "2026-02-01" });
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await InDatabase(async db => { Assert.Empty(db.CapitalRevisions); Assert.Empty(db.AuditLogs); Assert.Equal(100, (await db.Capitals.SingleAsync()).RegulatoryCapital); });
    }
    [SqlServerFact]
    public async Task BothEfAndSqlRejectHistoricalRevisionMutationAndDeletion()
    {
        await Save(new(2026, 1, 1), 100);
        await InDatabase(async db =>
        {
            var row = await db.CapitalRevisions.SingleAsync(); row.RegulatoryCapital = 200;
            Assert.Equal("capital.immutable_revision", (await Assert.ThrowsAsync<BusinessRuleException>(() => db.SaveChangesAsync())).Code);
        });
        await InDatabase(async db =>
        {
            var update = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync("UPDATE CapitalRevisions SET RegulatoryCapital = 200")); Assert.Equal(51008, update.Number);
            var deletion = await Assert.ThrowsAsync<SqlException>(() => db.CapitalRevisions.ExecuteDeleteAsync()); Assert.Equal(51008, deletion.Number);
        });
        await InDatabase(async db => { Assert.Equal(100, (await db.CapitalRevisions.SingleAsync()).RegulatoryCapital); Assert.Single(db.AuditLogs); });
    }
    [SqlServerFact]
    public async Task PopulatedUpgradeRetainsOnlyKnownLegacyDateAndDoesNotBackfillHistory()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = "RBBH_CAPITAL_UPGRADE_" + Guid.NewGuid().ToString("N") };
        await using var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync(db.Database.GetMigrations().Single(name => name.EndsWith("_EnforceReferenceAndPhysicalIdentityKeys")));
            await db.Database.ExecuteSqlRawAsync("INSERT Capitals (Id,RegulatoryCapital,CoreCapital,SupplementaryCapital,CapitalDate,CreatedBy,CreatedAt,IsActive) VALUES (NEWID(),100,50,25,'2026-01-01','original',SYSUTCDATETIME(),1)");
            await db.Database.MigrateAsync(); Assert.Empty(db.CapitalRevisions); Assert.Single(db.Capitals);
            var service = new CapitalService(db);
            Assert.Null((await service.AsOf(new(2025, 12, 31), default)).Value);
            Assert.Equal(100, (await service.AsOf(new(2026, 1, 1), default)).Value!.RegulatoryCapital);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
