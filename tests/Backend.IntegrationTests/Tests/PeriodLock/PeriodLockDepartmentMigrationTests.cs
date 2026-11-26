using IntegrationTests.Tests.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace IntegrationTests.Tests.PeriodLock;

public sealed class PeriodLockDepartmentMigrationTests
{
    [SqlServerFact]
    public async Task UpgradeRequiresReconciliationAndPreservesCanonicalLock()
    {
        var connection = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        {
            InitialCatalog = "RBBH_PERIOD_UPGRADE_" + Guid.NewGuid().ToString("N")
        };
        var options = new DbContextOptionsBuilder<ConnectedPartiesDbContext>()
            .UseSqlServer(connection.ConnectionString).Options;
        await using var db = new ConnectedPartiesDbContext(options);
        try
        {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261120140426_NormalizeRegistryColumnNames");

            var id = Guid.NewGuid();
            var created = DateTime.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO dbo.PeriodLocks (Id, Year, Month, IsLocked, Department, CreatedBy, CreatedAt, IsActive) VALUES ({id}, {created.Year}, {created.Month}, {true}, {"legacy"}, {"fixture"}, {created}, {true})");

            var exception = await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync());
            Assert.Equal(51000, exception.Number);

            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.PeriodLocks SET Department = NULL WHERE Id = {id}");
            await migrator.MigrateAsync();
            var row = await db.PeriodLocks.SingleAsync(item => item.Year == created.Year && item.Month == created.Month);
            Assert.True(row.IsLocked);
            Assert.True(row.Id > 0);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
