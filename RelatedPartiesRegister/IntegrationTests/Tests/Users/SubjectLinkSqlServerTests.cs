using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.IoC.Extensions.Authentication;

namespace IntegrationTests.Tests.Users;

public sealed class SubjectLinkSqlServerTests
{
    [SqlServerFact]
    public async Task UpgradeKeepsPlaceholderUntrustedAndApprovedFirstLinkPersistsWithAudit()
    {
        var databaseName = "RBBH_AUDIT_SUBJECT_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = databaseName };
        var options = new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options;
        await using var db = new ConnectedPartiesDbContext(options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20260929102823_AddGlobalCapital");
            var id = Guid.NewGuid();
            var placeholder = Guid.NewGuid().ToString();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AppUsers (Id, KeycloakId, Username, FirstName, LastName, Email, IsActive, CreatedAt, CreatedBy) VALUES ({id}, {placeholder}, {"fixture"}, {"Subject"}, {"Fixture"}, {"subject@example.test"}, {true}, {DateTime.UtcNow}, {"fixture"})");
            await db.Database.MigrateAsync();
            var migrated = await db.AppUsers.AsNoTracking().SingleAsync(user => user.Id == id);
            Assert.Equal(placeholder, migrated.KeycloakId);
            Assert.False(migrated.KeycloakSubjectConfirmed);
            Assert.Null(migrated.KeycloakSubjectLinkedAt);
            var principal = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim("sub", "real-fixture-subject"), new Claim(ClaimTypes.Email, "subject@example.test"), new Claim("email_verified", "true")], "Fixture"));
            Assert.Null((await new ApplicationIdentityResolver(db).ResolveAsync(principal)).User);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["IdentityBinding:AllowVerifiedEmailFirstLink"] = "true" }).Build();
            Assert.Equal(id, (await new ApplicationIdentityResolver(db, configuration).ResolveAsync(principal)).User!.Id);
            await using var fresh = new ConnectedPartiesDbContext(options);
            var linked = await fresh.AppUsers.SingleAsync(user => user.Id == id);
            Assert.Equal("real-fixture-subject", linked.KeycloakId);
            Assert.True(linked.KeycloakSubjectConfirmed);
            Assert.NotNull(linked.KeycloakSubjectLinkedAt);
            var audit = Assert.Single(await fresh.AuditLogs.ToListAsync());
            Assert.Equal("SUBJECT_LINK", audit.Action);
            Assert.Equal("subject@example.test", audit.Username);
            Assert.Contains(placeholder, audit.OldValues);
            Assert.Contains("real-fixture-subject", audit.NewValues);
        }
        finally
        {
            // Only the GUID-named disposable database created above is ever removed.
            await db.Database.EnsureDeletedAsync();
        }
    }
}
