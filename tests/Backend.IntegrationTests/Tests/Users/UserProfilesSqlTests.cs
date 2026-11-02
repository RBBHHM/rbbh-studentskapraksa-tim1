using System.Net;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Persistence;

namespace IntegrationTests.Tests.Users;

public sealed class UserProfilesSqlTests : AuthenticatedApiFixture
{
    protected override bool UseSql => true;
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IAppUserService, AppUserService>(); services.AddHttpClient<KeycloakAdminService>();
    }

    [SqlServerFact]
    public async Task ConcurrentNormalizedRegistrationCreatesExactlyOnePendingIdentityAndAudit()
    {
        using var second = NewClient("reviewer");
        var first = new { username = "new-user", firstName = "Fixture", lastName = "User", email = "person@external.example", roleIds = Array.Empty<Guid>() };
        var duplicate = new { username = " NEW-USER ", firstName = "Fixture", lastName = "User", email = "PERSON@external.example", roleIds = Array.Empty<Guid>() };
        var responses = await Task.WhenAll(Client.PostAsJsonAsync("/api/users", first), second.PostAsJsonAsync("/api/users", duplicate));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict);
        await InDatabase(async db =>
        {
            Assert.Equal(1, await db.AppUsers.CountAsync(user => user.Email.ToUpper() == "PERSON@EXTERNAL.EXAMPLE"));
            Assert.Equal(1, await db.AuditLogs.CountAsync(row => row.Action == "CREATE"));
        });
    }

    [SqlServerFact]
    public async Task UserCreationAuditFailureRollsBackTheNewIdentity()
    {
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER FixtureRejectUserCreateAudit ON AuditLogs AFTER INSERT AS
            BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action = 'CREATE') THROW 51000, 'fixture audit failure', 1; END
            """));
        var response = await Client.PostAsJsonAsync("/api/users", new
        { username = "new-user", firstName = "Fixture", lastName = "User", email = "person@external.example", roleIds = Array.Empty<Guid>() });
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await InDatabase(async db => { Assert.False(await db.AppUsers.AnyAsync(user => user.Username == "new-user")); Assert.Empty(db.AuditLogs); });
    }

    [SqlServerFact]
    public async Task IdentityConstraintPreflightRejectsDuplicatesWithoutDeletingOrReassigningData()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable))
        { InitialCatalog = "RBBH_IDENTITY_PREFLIGHT_" + Guid.NewGuid().ToString("N") };
        await using var db = new ConnectedPartiesDbContext(new DbContextOptionsBuilder<ConnectedPartiesDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync(db.Database.GetMigrations().Single(name => name.EndsWith("_AddRoleRequestsAndNotifications")));
            foreach (var name in new[] { "first", "second" })
            {
                var email = name == "first" ? "same@example.test" : " same@example.test";
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AppUsers (Id,KeycloakId,KeycloakSubjectConfirmed,Username,FirstName,LastName,Email,IsActive,CreatedAt,CreatedBy) VALUES ({Guid.NewGuid()},{name},{false},{name},{"Fixture"},{"User"},{email},{true},{DateTime.UtcNow},{"fixture"})");
            }
            var exception = await Assert.ThrowsAsync<SqlException>(() => db.Database.MigrateAsync());
            Assert.Contains("preflight", exception.Message);
            Assert.Equal(2, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM AppUsers").SingleAsync());
            Assert.DoesNotContain(await db.Database.GetAppliedMigrationsAsync(), name => name.EndsWith("_EnforceNormalizedApplicationUsers"));
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
