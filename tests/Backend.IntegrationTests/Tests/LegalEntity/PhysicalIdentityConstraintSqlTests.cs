using IntegrationTests.Tests.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace IntegrationTests.Tests.LegalEntity;

public sealed class PhysicalIdentityConstraintSqlTests
{
    private static async Task<ConnectedPartiesDbContext> Database()
    {
        var name = "RBBH_CASE_KEYS_" + Guid.NewGuid().ToString("N");
        var options = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable)) { InitialCatalog = "master" };
        await using (var connection = new SqlConnection(options.ConnectionString))
        {
            await connection.OpenAsync(); await using var command = connection.CreateCommand();
            // Identifier is exclusively this fixture's GUID name, never request/config input.
            command.CommandText = "CREATE DATABASE [" + name + "] COLLATE Latin1_General_100_CS_AS"; await command.ExecuteNonQueryAsync();
        }
        options.InitialCatalog = name;
        var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(options.ConnectionString).Options);
        await db.GetService<IMigrator>().MigrateAsync(db.Database.GetMigrations().Single(name => name.EndsWith("_AddRegistryGccAndExposure")));
        return db;
    }
    private static Task Insert(ConnectedPartiesDbContext db, string passport, bool active) => db.Database.ExecuteSqlInterpolatedAsync($"INSERT RelatedPersons (Id,FirstName,LastName,Residency,Status,CreatedAt,CreatedBy,IsActive,IsIdentifiedStaff,DeclarationNoFamilyMembers,ConnectedWithBank,SpecialRelationshipWithBank,SpecialContract,MalusClawback,PassportNumber) VALUES ({Guid.NewGuid()},{"Fixture"},{"Person"},{"NonResident"},{"Draft"},{DateTime.UtcNow},{"fixture"},{active},{true},{false},{false},{false},{false},{false},{passport})");
    [SqlServerFact]
    public async Task PopulatedPreflightRejectsCaseDuplicatesWithoutDeletingEitherRow()
    {
        await using var db = await Database();
        try
        {
            await Insert(db, "ab12345", true); await Insert(db, "AB12345", true);
            var error = await Assert.ThrowsAsync<SqlException>(() => db.Database.MigrateAsync()); Assert.Equal(51006, error.Number);
            await using var connection = new SqlConnection(db.Database.GetConnectionString()); await connection.OpenAsync();
            await using var command = connection.CreateCommand(); command.CommandText = "SELECT COUNT(*) FROM RelatedPersons";
            Assert.Equal(2, Convert.ToInt32(await command.ExecuteScalarAsync()));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
    [SqlServerFact]
    public async Task DefinitionNamePreflightRetainsDifferentlyCasedLegacyDefinitions()
    {
        await using var db = await Database();
        try
        {
            await db.Database.ExecuteSqlRawAsync("INSERT CodeListDefinitions (Id,Name,IsActive,CreatedAt,CreatedBy,IsSystemRequired,Version) VALUES (NEWID(),'languages',1,SYSUTCDATETIME(),'fixture',0,0)");
            var error = await Assert.ThrowsAsync<SqlException>(() => db.Database.MigrateAsync()); Assert.Equal(51005, error.Number);
            Assert.Equal(2, await db.CodeListDefinitions.CountAsync(row => row.Name.ToUpper() == "LANGUAGES"));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
    [SqlServerFact]
    public async Task FbaPreflightDoesNotSilentlyTrimOrMergeExistingBusinessRecords()
    {
        await using var db = await Database();
        try
        {
            await Insert(db, "AB12345", true); await Insert(db, "CD12345", true);
            await db.Database.ExecuteSqlRawAsync("UPDATE RelatedPersons SET FBAId = CASE WHEN PassportNumber = 'AB12345' THEN '12345' ELSE ' 12345' END");
            var error = await Assert.ThrowsAsync<SqlException>(() => db.Database.MigrateAsync()); Assert.Equal(51007, error.Number);
            Assert.Equal(2, await db.RelatedPersons.CountAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    [SqlServerFact]
    public async Task NormalizedUniqueIndexProtectsCaseSensitiveDatabaseAndPreservesInactiveReuse()
    {
        await using var db = await Database();
        try
        {
            await Insert(db, "ab12345", true); await db.Database.MigrateAsync();
            var error = await Assert.ThrowsAsync<SqlException>(() => Insert(db, "AB12345", true)); Assert.Contains(error.Number, new[] { 2601, 2627 });
            await Insert(db, "AB12345", false);
            Assert.Equal(2, await db.RelatedPersons.IgnoreQueryFilters().CountAsync()); Assert.Single(db.RelatedPersons);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
