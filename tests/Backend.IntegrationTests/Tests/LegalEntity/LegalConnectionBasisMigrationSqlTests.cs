using IntegrationTests.Tests.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RBBH.ConnectedParties.DL.Persistence;

namespace IntegrationTests.Tests.LegalEntity;

public sealed class LegalConnectionBasisMigrationSqlTests
{
    [SqlServerFact]
    public async Task SeedWorksWhenDatabaseAndCodebookKeysUseDifferentCollations()
    {
        var name = "RBBH_BASIS_COLLATION_" + Guid.NewGuid().ToString("N");
        var connectionString = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        {
            InitialCatalog = "master"
        };
        await using (var connection = new SqlConnection(connectionString.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{name}] COLLATE SQL_Latin1_General_CP1_CI_AS";
            await command.ExecuteNonQueryAsync();
        }

        connectionString.InitialCatalog = name;
        await using var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>()
            .UseSqlServer(connectionString.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20261127093000_IndexRegistryReadPaths");
            await db.Database.MigrateAsync();

            var basis = await db.CodeLists.AsNoTracking()
                .Where(row => row.Definition!.Name == "OSNOV_POVEZANOSTI" && row.Kod == "ZOB-2-V-8")
                .SingleAsync();
            Assert.False(string.IsNullOrWhiteSpace(basis.Opis));
            var translations = await db.CodeLists.AsNoTracking()
                .Where(row => row.Definition!.Name == "LANGUAGE_PACKS" && row.Localization!.ResourceKey == "legal.basis.zob-2-v-8")
                .Select(row => row.Localization!.LanguageCode)
                .ToListAsync();
            Assert.Equal(new[] { "BS", "DE", "EN" }, translations.Order(StringComparer.Ordinal));
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
