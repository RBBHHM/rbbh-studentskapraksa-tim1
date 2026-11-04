using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IntegrationTests.Tests.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace IntegrationTests.Tests;

public sealed class LimitImportSqlTests : LimitImportFixture
{
    protected override bool UseSql => true;
    [SqlServerFact]
    public async Task CreationAndImportRollbackGeneratedRowsWhenAuditFails()
    {
        await Seed(); await Create(); var bytes = await File(200);
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FixtureLimitAuditFailure ON AuditLogs AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE TableName = 'Limit') THROW 51000, 'fixture failure', 1; END"));
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.PostAsJsonAsync("/api/limiti", Body(300))).StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, (await Import(bytes)).StatusCode);
        await InDatabase(async db => { Assert.Single(db.Limiti); Assert.Single(db.AuditLogs); Assert.Equal(100.50m, (await db.Limiti.SingleAsync()).OdobreniLimit); });
    }
    [SqlServerFact]
    public async Task UpdateAndDeleteRetainOriginalDataIfAuditCannotBeWritten()
    {
        await Seed(); var id = await Create();
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FixtureLimitAuditFailure ON AuditLogs AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action IN ('UPDATE','DELETE')) THROW 51000, 'fixture failure', 1; END"));
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.PutAsJsonAsync($"/api/limiti/{id}", Body(200))).StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.DeleteAsync($"/api/limiti/{id}")).StatusCode);
        await InDatabase(async db => { Assert.Equal(100.50m, (await db.Limiti.SingleAsync()).OdobreniLimit); Assert.Single(db.AuditLogs); });
    }
    [SqlServerFact]
    public async Task ConcurrentIdenticalImportsCommitOnlyOneBusinessRowAndAudit()
    {
        await Seed(); await Create(); var bytes = await File(200);
        var results = await Task.WhenAll(Import(bytes), Import(bytes)); var imported = 0;
        foreach (var response in results)
            if (response.StatusCode == HttpStatusCode.OK) imported += (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("imported").GetInt32();
            else Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, imported);
        await InDatabase(async db => { Assert.Equal(2, await db.Limiti.CountAsync()); Assert.Equal(2, await db.AuditLogs.CountAsync()); Assert.Single(await db.Limiti.Where(row => row.ImportFingerprint != null).ToListAsync()); });
    }
    [SqlServerFact]
    public async Task NullableImportKeyMigrationRetainsAllPreexistingLimitsWithoutBackfill()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = "RBBH_LIMIT_UPGRADE_" + Guid.NewGuid().ToString("N") };
        await using var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync(db.Database.GetMigrations().Single(name => name.EndsWith("_AddReportSnapshotProvenance")));
            var party = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT LegalEntities (Id,IsResident,Name,BasisOfConnection,Status,CreatedBy,CreatedAt,IsActive) VALUES ({party},{false},{"Fixture"},{"VL"},{"Draft"},{"original"},{DateTime.UtcNow},{true})");
            for (var index = 0; index < 2; index++)
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Limiti (LegalEntityId,TipLimita,OdobreniLimit,CreatedBy,CreatedAt) VALUES ({party},{"TYPE"},{100.50m},{"original"},{DateTime.UtcNow})");
            await db.Database.MigrateAsync(); var rows = await db.Limiti.ToListAsync(); Assert.Equal(2, rows.Count);
            Assert.All(rows, row => { Assert.Null(row.ImportFingerprint); Assert.Equal(100.50m, row.OdobreniLimit); Assert.Equal("original", row.CreatedBy); });
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
