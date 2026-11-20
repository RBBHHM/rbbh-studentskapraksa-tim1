using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;
using RBBH.ConnectedParties.DL.Persistence;

namespace UnitTests.Authentication;

public sealed class GlobalPeriodContractTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanonicalGlobalRowIsAuthoritative(bool locked)
    {
        await using var db = Database();
        var now = DateTime.UtcNow;
        db.PeriodLocks.Add(new PeriodLock { Year = now.Year, Month = now.Month, IsLocked = locked });
        await db.SaveChangesAsync();
        Assert.Equal(locked, await new PeriodLockRepository(db).IsCurrentPeriodLockedAsync());
    }

    [Fact]
    public async Task StaleUpdateCannotOverwriteANewerPeriodLock()
    {
        var options = new DbContextOptionsBuilder<ConnectedPartiesDbContext>()
            .UseInMemoryDatabase("period-concurrency-" + Guid.NewGuid()).Options;
        await using var first = new ConnectedPartiesDbContext(options);
        await using var second = new ConnectedPartiesDbContext(options);
        var original = new PeriodLock { Year = 2026, Month = 11 };
        first.PeriodLocks.Add(original);
        await first.SaveChangesAsync();

        var stale = await second.PeriodLocks.SingleAsync();
        original.IsLocked = true;
        original.ModifiedAt = DateTime.UtcNow;
        await first.SaveChangesAsync();

        stale.ModifiedAt = DateTime.UtcNow;
        stale.ModifiedBy = "stale-writer";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    private static ConnectedPartiesDbContext Database() => new(new DbContextOptionsBuilder<ConnectedPartiesDbContext>()
        .UseInMemoryDatabase("global-contract-" + Guid.NewGuid()).Options);
}
