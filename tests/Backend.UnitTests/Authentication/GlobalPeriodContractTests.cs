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

    private static ConnectedPartiesDbContext Database() => new(new DbContextOptionsBuilder<ConnectedPartiesDbContext>()
        .UseInMemoryDatabase("global-contract-" + Guid.NewGuid()).Options);
}
