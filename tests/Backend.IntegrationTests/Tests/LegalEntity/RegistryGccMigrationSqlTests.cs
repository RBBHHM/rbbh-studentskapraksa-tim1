using IntegrationTests.Tests.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace IntegrationTests.Tests.LegalEntity;

public sealed class RegistryGccMigrationSqlTests
{
    [SqlServerFact]
    public async Task PopulatedUpgradePreservesExistingRecordAndDoesNotInventHistoricalExposureOrGcc()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = "RBBH_GCC_UPGRADE_" + Guid.NewGuid().ToString("N") };
        await using var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync(db.Database.GetMigrations().Single(name => name.EndsWith("_AddRuntimeLocalizationCodebooks")));
            await db.Database.ExecuteSqlRawAsync("INSERT LegalEntities (Id,IsResident,Name,BasisOfConnection,Status,CreatedBy,CreatedAt,IsActive) VALUES (NEWID(),0,N'Historical fixture','VL','Draft','original',SYSUTCDATETIME(),1)");
            await db.Database.MigrateAsync(); var row = await db.LegalEntities.SingleAsync();
            Assert.Equal("Historical fixture", row.Name); Assert.Equal("original", row.CreatedBy);
            Assert.Null(row.GccNumber); Assert.Null(row.GccName); Assert.Null(row.IsLargeExposure); Assert.Null(row.CreatorSubject);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
