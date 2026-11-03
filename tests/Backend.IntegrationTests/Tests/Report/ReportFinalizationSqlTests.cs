using System.Net;
using IntegrationTests.Tests.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace IntegrationTests.Tests.Report;

public sealed class ReportFinalizationSqlTests : ReportFinalizationFixture
{
    protected override bool UseSql => true;
    [SqlServerFact]
    public async Task SnapshotAndProvenanceRollbackWhenGenerationAuditFails()
    {
        await SeedExposure();
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FixtureReportAuditFailure ON AuditLogs AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE TableName = 'Report') THROW 51000, 'fixture failure', 1; END"));
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.PostAsync("/api/reports/daily", null)).StatusCode);
        await InDatabase(async db => { Assert.Empty(db.Reports); Assert.Empty(db.AuditLogs); Assert.Single(db.ClientLimits); });
    }
    [SqlServerFact]
    public async Task PopulatedProvenanceMigrationPreservesHistoricalSnapshotWithoutInventingSource()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = "RBBH_REPORT_UPGRADE_" + Guid.NewGuid().ToString("N") };
        await using var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync(db.Database.GetMigrations().Single(name => name.EndsWith("_AddImmutableCapitalHistory")));
            const string snapshot = "[{\"Naziv\":\"Historical fixture\",\"CurrentExposure\":123.45}]";
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT Reports (Id,ReportType,ReportDate,TotalClients,ClientsWithBreachedLimit,TotalExposure,DataSnapshot,CreatedBy,CreatedAt,IsActive) VALUES ({Guid.NewGuid()},{"MONTHLY"},{new DateTime(2026, 9, 1)},{1},{0},{123.45m},{snapshot},{"original"},{DateTime.UtcNow},{true})");
            await db.Database.MigrateAsync(); var row = await db.Reports.SingleAsync();
            Assert.Equal(snapshot, row.DataSnapshot); Assert.Equal(123.45m, row.TotalExposure); Assert.Equal("original", row.CreatedBy); Assert.Null(row.DataSource);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
