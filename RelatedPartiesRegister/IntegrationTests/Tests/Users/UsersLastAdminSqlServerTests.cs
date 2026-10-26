using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;

namespace IntegrationTests.Tests.Users;

/// <summary>
/// Runs only when RBBH_TEST_SQLSERVER names a SQL Server instance WITHOUT an initial catalog,
/// e.g. <c>Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true</c>.
/// Every test creates and drops its own uniquely named database; no existing database is touched.
/// Without the variable the tests are reported as skipped, never as passed.
/// </summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public const string Variable = "RBBH_TEST_SQLSERVER";

    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variable)))
            Skip = $"Set {Variable} to a disposable SQL Server instance (see UsersLastAdminSqlServerTests).";
    }
}

public sealed class UsersLastAdminSqlServerTests : UsersHttpFixture
{
    private readonly string _databaseName = "RBBH_AUDIT_LASTADMIN_" + Guid.NewGuid().ToString("N");
    private string? _connectionString;

    protected override void ConfigureDatabase(DbContextOptionsBuilder options)
    {
        var instance = Environment.GetEnvironmentVariable(SqlServerFactAttribute.Variable);
        if (string.IsNullOrWhiteSpace(instance))
        {
            // Skipped test: the fixture still initializes, so keep it harmless.
            options.UseInMemoryDatabase("sql-tests-skipped-" + Guid.NewGuid());
            return;
        }
        var builder = new SqlConnectionStringBuilder(instance) { InitialCatalog = _databaseName };
        _connectionString = builder.ConnectionString;
        // Same execution strategy as production (DatabaseExtensions).
        options.UseSqlServer(_connectionString, sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
    }

    protected override async Task PrepareDatabaseAsync(ConnectedPartiesDbContext db)
    {
        if (_connectionString is not null)
            await db.Database.EnsureCreatedAsync(); // disposable database only
    }

    protected override async Task CleanUpDatabaseAsync()
    {
        if (_connectionString is null) return;
        SqlConnection.ClearAllPools();
        // Drop only the uniquely named database this fixture created.
        var master = new SqlConnectionStringBuilder(_connectionString) { InitialCatalog = "master" }.ConnectionString;
        await using var connection = new SqlConnection(master);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID(N'{_databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]; END";
        await command.ExecuteNonQueryAsync();
    }

    private async Task<(HttpResponseMessage First, HttpResponseMessage Second, Guid OtherId)> RaceAsync(string firstAction, string secondAction)
    {
        var otherId = await AddUser(ApplicationAccessRoles.Administrator, "other@example.test");
        using var second = _app.GetTestClient();
        second.DefaultRequestHeaders.Add("X-Test-Email", "other@example.test");
        await using (var lookup = _app.Services.CreateAsyncScope())
            second.DefaultRequestHeaders.Add("X-Test-Subject", (await lookup.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>()
                .AppUsers.SingleAsync(user => user.Id == otherId)).KeycloakId);
        _client.DefaultRequestHeaders.Add("X-Test-Race", "true");
        second.DefaultRequestHeaders.Add("X-Test-Race", "true");
        // Each admin acts on the other one; both pass authentication before either mutates.
        var results = await Task.WhenAll(Mutate(_client, otherId, firstAction), Mutate(second, _localId, secondAction));
        return (results[0], results[1], otherId);
    }

    [SqlServerFact]
    public async Task RunsOnSqlServerWithTheProductionApplicationLockPath()
    {
        await using var scope = _app.Services.CreateAsyncScope();
        Assert.True(scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>().Database.IsSqlServer());
    }

    [SqlServerFact]
    public async Task ConcurrentMutualAdminRemovalLeavesExactlyOneActiveAdmin()
    {
        var (first, second, _) = await RaceAsync("empty", "empty");
        AssertOneWinnerOneRejected(first, second);
        await AssertExactlyOneActiveAdminAsync();
    }

    [SqlServerFact]
    public async Task ConcurrentDeactivateAndPermanentDeleteLeaveExactlyOneActiveAdmin()
    {
        var (first, second, _) = await RaceAsync("deactivate", "delete");
        AssertOneWinnerOneRejected(first, second);
        await AssertExactlyOneActiveAdminAsync();
    }

    [SqlServerFact]
    public async Task ConcurrentDemotionAndDeactivationLeaveExactlyOneActiveAdmin()
    {
        var (first, second, _) = await RaceAsync("role", "deactivate");
        AssertOneWinnerOneRejected(first, second);
        await AssertExactlyOneActiveAdminAsync();
    }

    [SqlServerFact]
    public async Task LockIsReleasedAfterEachMutationSoLaterRequestsAreNotStuckBusy()
    {
        var otherId = await AddUser(ApplicationAccessRoles.PhysicalPersons, "ordinary@example.test");
        Assert.Equal(HttpStatusCode.OK, (await Mutate(_client, otherId, "deactivate")).StatusCode);
        var another = await AddUser(ApplicationAccessRoles.PhysicalPersons, "ordinary2@example.test");
        Assert.Equal(HttpStatusCode.OK, (await Mutate(_client, another, "delete")).StatusCode);
        // A rejected (final-Admin) mutation must also release the transaction-owned lock.
        Assert.Equal(HttpStatusCode.BadRequest, (await Mutate(_client, _localId, "deactivate")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Mutate(_client, await AddUser(ApplicationAccessRoles.Limits, "third@example.test"), "delete")).StatusCode);
    }

    [SqlServerFact]
    public async Task UnconfirmedPlaceholderCannotSubstituteForTheFinalConfirmedAdminOnSqlServer()
    {
        await AddDatabaseOperator();
        _client.DefaultRequestHeaders.Add("X-Test-Email", "operator@example.test");
        var placeholder = await AddUser(ApplicationAccessRoles.Administrator, "unconfirmed@example.test");
        await using (var scope = _app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
            (await db.AppUsers.SingleAsync(user => user.Id == placeholder)).KeycloakSubjectConfirmed = false;
            await db.SaveChangesAsync();
        }
        foreach (var action in new[] { "role", "empty", "deactivate", "delete" })
            Assert.Equal(HttpStatusCode.Conflict, (await Mutate(_client, _localId, action)).StatusCode);
        await using var final = _app.Services.CreateAsyncScope();
        var finalDb = final.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        Assert.True((await finalDb.AppUsers.SingleAsync(user => user.Id == _localId)).IsActive);
        Assert.True((await finalDb.AppUsers.SingleAsync(user => user.Id == _localId)).KeycloakSubjectConfirmed);
        Assert.Empty(await finalDb.AuditLogs.ToListAsync());
    }

    private static void AssertOneWinnerOneRejected(HttpResponseMessage first, HttpResponseMessage second)
    {
        var codes = new[] { first.StatusCode, second.StatusCode };
        Assert.Single(codes, code => code == HttpStatusCode.OK);
        // Either the final-Admin rule (409) or a busy application lock (409) rejects the loser.
        Assert.Single(codes, code => code == HttpStatusCode.Conflict);
    }

    private async Task AssertExactlyOneActiveAdminAsync()
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ConnectedPartiesDbContext>();
        var activeAdmins = await db.AppUsers.Where(user => user.IsActive)
            .Join(db.UserRoles.Where(mapping => mapping.IsActive), user => user.Id, mapping => mapping.UserId, (user, mapping) => mapping.RoleId)
            .Join(db.Roles.Where(role => role.Name == ApplicationAccessRoles.Administrator && role.IsActive), roleId => roleId, role => role.Id, (roleId, role) => roleId)
            .CountAsync();
        Assert.Equal(1, activeAdmins);
    }
}
