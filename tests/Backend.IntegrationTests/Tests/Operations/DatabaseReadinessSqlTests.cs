using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.IoC.Extensions.Health;
using IntegrationTests.Tests.Users;

namespace IntegrationTests.Tests.Operations;

public sealed class DatabaseReadinessSqlTests
{
    [SqlServerFact]
    public async Task ConfiguredDisposableSqlDatabaseActuallyParticipatesInReadiness()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        {
            InitialCatalog = "RBBH_OPS_HEALTH_" + Guid.NewGuid().ToString("N")
        };
        await using var database = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>()
            .UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await database.Database.MigrateAsync(); // This test's GUID database only.
            var result = await new DatabaseHealthCheck(database).CheckHealthAsync(new HealthCheckContext());
            Assert.Equal(HealthStatus.Healthy, result.Status);
        }
        finally
        {
            await database.Database.EnsureDeletedAsync();
        }
    }
    [SqlServerFact]
    public async Task ReadinessRejectsAnUnmigratedDatabaseWithoutApplyingDdl()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = "RBBH_OPS_PENDING_" + Guid.NewGuid().ToString("N") };
        await using var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.Database.EnsureCreatedAsync(); // Current tables without deployment migration history are not a verified upgrade.
            var before = (await db.Database.GetPendingMigrationsAsync()).ToArray(); Assert.NotEmpty(before);
            var result = await new DatabaseHealthCheck(db).CheckHealthAsync(new HealthCheckContext());
            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Equal(before, (await db.Database.GetPendingMigrationsAsync()).ToArray());
            Assert.Empty(await db.Database.GetAppliedMigrationsAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

}
