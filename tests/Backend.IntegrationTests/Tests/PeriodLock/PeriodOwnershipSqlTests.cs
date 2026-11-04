using System.Net;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;
using IntegrationTests.Tests.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;

namespace IntegrationTests.Tests.PeriodLock;

public sealed class PeriodOwnershipSqlTests : AuthenticatedApiFixture
{
    protected override bool UseSql => true;
    private readonly ResponseBarrier _barrier = new();
    protected override void ConfigureDatabase(DbContextOptionsBuilder options) => options.AddInterceptors(_barrier);
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IUnlockRequestRepository, UnlockRequestRepository>();
        services.AddScoped<IPeriodLockService, PeriodLockService>();
        services.AddSingleton(Mock.Of<IAppUserService>());
    }

    private async Task<Guid> Seed()
    {
        var request = new UnlockRequest { RequestedBySubject = "creator", RequestedBy = "creator", RequestedByEmail = "creator@example.test",
            Year = 2026, Month = 10, Reason = "Original request reason", Status = "NEEDS_INFO", CreatedBy = "fixture" };
        await InDatabase(async db => { db.UnlockRequests.Add(request); await db.SaveChangesAsync(); });
        return request.Id;
    }

    [SqlServerFact]
    public async Task RacingResponsesPersistOneChildAndOneAudit()
    {
        var id = await Seed(); using var other = NewClient("creator");
        _barrier.Enabled = true;
        var body = new { message = "Concurrent response text" };
        var results = await Task.WhenAll(Client.PostAsJsonAsync($"/api/period-lock/unlock-requests/{id}/respond", body),
            other.PostAsJsonAsync($"/api/period-lock/unlock-requests/{id}/respond", body));
        _barrier.Enabled = false;
        Assert.Single(results, result => result.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, result => result.StatusCode == HttpStatusCode.Conflict);
        await InDatabase(async db => { Assert.Single(await db.UnlockRequests.Where(row => row.ParentRequestId == id).ToListAsync()); Assert.Single(db.AuditLogs); });
    }

    [SqlServerFact]
    public async Task ResponseAuditFailureRollsBackChildAndKeepsOriginalWorkflow()
    {
        var id = await Seed();
        await InDatabase(db => db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER FixtureRejectResponseAudit ON AuditLogs AFTER INSERT AS
            BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE Action = 'UNLOCK_REQUEST') THROW 51000, 'fixture audit failure', 1; END
            """));
        Assert.Equal(HttpStatusCode.InternalServerError, (await Client.PostAsJsonAsync($"/api/period-lock/unlock-requests/{id}/respond", new { message = "Response with failed audit" })).StatusCode);
        await InDatabase(async db => { Assert.Equal("NEEDS_INFO", (await db.UnlockRequests.SingleAsync()).Status); Assert.Empty(db.AuditLogs); });
    }

    private sealed class ResponseBarrier : SaveChangesInterceptor
    {
        internal bool Enabled;
        private int _arrived;
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Enabled && eventData.Context!.ChangeTracker.Entries<UnlockRequest>().Any(entry => entry.State == EntityState.Added && entry.Entity.ParentRequestId != null))
            {
                if (Interlocked.Increment(ref _arrived) == 2) _ready.TrySetResult();
                await _ready.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            return result;
        }
    }
}
