using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.DTO.Roles;
using RBBH.ConnectedParties.DL.Persistence;

namespace IntegrationTests.Tests.Users;

public sealed class RoleCatalogSqlTests : RoleCatalogFixture
{
    protected override bool UseSql => true;

    [SqlServerFact]
    public async Task ConcurrentCatalogUpdatesCommitOneVersionAndOneAudit()
    {
        var role = await CreateCatalog(); using var other = NewClient("reviewer");
        var input = new UpdateRoleCatalogDTO("Concurrent", "Retained", true, 1, []);
        var results = await Task.WhenAll(Client.PutAsJsonAsync($"/api/role-catalog/{role.Id}", input), other.PutAsJsonAsync($"/api/role-catalog/{role.Id}", input));
        Assert.Single(results, row => row.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, row => row.StatusCode == HttpStatusCode.Conflict);
        await InDatabase(async db =>
        {
            Assert.Equal(2, (await db.Roles.SingleAsync(row => row.Id == role.Id)).Version);
            Assert.Empty(await db.RolePermissions.Where(row => row.RoleId == role.Id).ToListAsync());
            Assert.Equal(1, await db.AuditLogs.CountAsync(row => row.Action == "ROLE_CATALOG_UPDATE"));
        });
    }

    [SqlServerFact]
    public async Task CatalogAuditFailureRollsBackDefinitionAndPermissionRemoval()
    {
        var role = await CreateCatalog();
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER FixtureRejectCatalogAudit ON AuditLogs AFTER INSERT AS
            BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action = 'ROLE_CATALOG_UPDATE') THROW 51000, 'fixture audit failure', 1; END
            """));
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.PutAsJsonAsync($"/api/role-catalog/{role.Id}", new UpdateRoleCatalogDTO("Changed", null, false, 1, []))).StatusCode);
        await InDatabase(async db =>
        {
            var stored = await db.Roles.SingleAsync(row => row.Id == role.Id);
            Assert.Equal(role.DisplayName, stored.DisplayName); Assert.True(stored.IsActive); Assert.Equal(1, stored.Version);
            Assert.Equal(role.PermissionIds, await db.RolePermissions.Where(row => row.RoleId == role.Id).Select(row => row.PermissionId).ToArrayAsync());
            Assert.Single(db.AuditLogs);
        });
    }

    [SqlServerFact]
    public async Task CatalogUpgradeRetainsExistingRoleAndGrantAndRejectsNormalizedDuplicateWithoutDeletingData()
    {
        foreach (var duplicate in new[] { false, true })
        {
            var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
            { InitialCatalog = "RBBH_ROLE_UPGRADE_" + Guid.NewGuid().ToString("N") };
            await using var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options);
            try
            {
                await db.GetService<IMigrator>().MigrateAsync("20261103083033_EnforceOpaqueSubjectComparisons");
                var roleId = Guid.NewGuid(); var permissionId = Guid.NewGuid();
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Roles(Id,Name,IsActive,CreatedAt,CreatedBy) VALUES({roleId},{"existing-role"},{true},{DateTime.UtcNow},{"original"})");
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Permissions(Id,Code,Module,Description,IsActive) VALUES({permissionId},{"FIXTURE_VIEW"},{"fixture"},{"Original description"},{true})");
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RolePermissions(RoleId,PermissionId) VALUES({roleId},{permissionId})");
                if (duplicate)
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Roles(Id,Name,IsActive,CreatedAt,CreatedBy) VALUES({Guid.NewGuid()},{" existing-role"},{true},{DateTime.UtcNow},{"original"})");
                    var error = await Assert.ThrowsAsync<SqlException>(() => db.Database.MigrateAsync()); Assert.Equal(51010, error.Number);
                    Assert.Equal(2, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM Roles").SingleAsync());
                    Assert.DoesNotContain(await db.Database.GetAppliedMigrationsAsync(), name => name.EndsWith("AddAdministrativeRoleCatalog"));
                }
                else
                {
                    await db.Database.MigrateAsync();
                    var role = await db.Roles.SingleAsync(); Assert.Equal(roleId, role.Id); Assert.Equal("existing-role", role.Name);
                    Assert.Null(role.DisplayName); Assert.Null(role.Description); Assert.Equal(1, role.Version);
                    Assert.Equal(permissionId, (await db.RolePermissions.SingleAsync()).PermissionId);
                    Assert.Equal("original", role.CreatedBy);
                }
            }
            finally { await db.Database.EnsureDeletedAsync(); }
        }
    }
}
