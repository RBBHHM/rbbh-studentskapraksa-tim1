using System.Data.Common;
using System.Diagnostics;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;
using IntegrationTests.Tests.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.Users;
using RBBH.ConnectedParties.DL.Entities.Users;
using Xunit.Abstractions;

namespace IntegrationTests.Tests.Operations;

public sealed class PagedUsersPerformanceSqlTests(ITestOutputHelper output) : AuthenticatedApiFixture
{
    protected override bool UseSql => true;
    private readonly QueryCounter _counter = new();
    protected override void ConfigureDatabase(DbContextOptionsBuilder options) => options.AddInterceptors(_counter);
    protected override void ConfigureServices(IServiceCollection services)
    { services.AddScoped<IAppUserService, AppUserService>(); services.AddHttpClient<KeycloakAdminService>(); }

    [SqlServerFact]
    public async Task PagedSqlReadsHaveBoundedQueryCountForOneAndOneHundredRowsWithMultipleRoles()
    {
        const int total = 2500;
        await InDatabase(async db =>
        {
            var roles = await db.Roles.Where(row => row.Name == "FL" || row.Name == "PL").Select(row => row.Id).ToArrayAsync();
            Assert.Equal(2, roles.Length);
            var users = Enumerable.Range(0, total).Select(index => new AppUser
            { KeycloakId = $"perf-subject-{index}", KeycloakSubjectConfirmed = true, Username = $"perf-person-{index:D5}",
                FirstName = "Synthetic", LastName = $"Person-{index:D5}", Email = $"perf-person-{index}@example.test" }).ToArray();
            db.AppUsers.AddRange(users);
            foreach (var user in users)
            foreach (var role in roles) db.UserRoles.Add(new() { UserId = user.Id, RoleId = role });
            await db.SaveChangesAsync();
        });
        var commandCounts = new List<int>();
        foreach (var size in new[] { 1, 100 })
        {
            _counter.Reset(); var watch = Stopwatch.StartNew();
            var page = await Client.GetFromJsonAsync<GetUsersResponseDTO>($"/api/users?search=perf-person&page=2&pageSize={size}");
            watch.Stop(); var commands = _counter.Stop(); commandCounts.Add(commands);
            Assert.Equal(total, page!.Total); Assert.Equal(size, page.Users.Count); Assert.Equal(2, page.Page);
            Assert.All(page.Users, user => { Assert.Equal(2, user.Roles.Count); Assert.Equal(user.EffectivePermissions.Distinct().Count(), user.EffectivePermissions.Count); });
            Assert.InRange(commands, 1, 10); // Includes real authorization DB reads; detects per-user lookup/grant queries.
            Assert.True(_counter.SawOffsetFetch);
            output.WriteLine("Synthetic SQL dataset: {0} users / {1} assignments; pageSize={2}; SQL reader commands={3}; HTTP elapsed={4:F1}ms (not a production SLO).",
                total, total * 2, size, commands, watch.Elapsed.TotalMilliseconds);
        }
        Assert.InRange(Math.Abs(commandCounts[1] - commandCounts[0]), 0, 1);
    }

    private sealed class QueryCounter : DbCommandInterceptor
    {
        private int _commands;
        private bool _enabled;
        internal bool SawOffsetFetch { get; private set; }
        internal void Reset() { _commands = 0; SawOffsetFetch = false; _enabled = true; }
        internal int Stop() { _enabled = false; return _commands; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (_enabled)
            {
                Interlocked.Increment(ref _commands);
                if (command.CommandText.Contains("OFFSET", StringComparison.OrdinalIgnoreCase) && command.CommandText.Contains("FETCH", StringComparison.OrdinalIgnoreCase)) SawOffsetFetch = true;
            }
            return ValueTask.FromResult(result);
        }
    }
}
