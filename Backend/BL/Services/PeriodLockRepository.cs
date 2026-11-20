using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;
using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace RBBH.ConnectedParties.BL.Services;

public class PeriodLockRepository(ConnectedPartiesDbContext db) : IPeriodLockRepository
{
    public async Task<PeriodLock?> GetCurrentAsync()
    {
        var now = DateTime.UtcNow;
        return await GetByPeriodAsync(now.Year, now.Month);
    }

    public async Task<PeriodLock?> GetByPeriodAsync(int year, int month)
    {
        return await db.PeriodLocks
            .Where(p => p.Year == year && p.Month == month)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsCurrentPeriodLockedAsync()
    {
        var now = DateTime.UtcNow;
        return await IsPeriodLockedAsync(now.Year, now.Month);
    }

    public async Task<bool> IsPeriodLockedAsync(int year, int month)
    {
        var now = DateTime.UtcNow;
        bool isPast = year < now.Year || (year == now.Year && month < now.Month);

        var globalLock = await GetByPeriodAsync(year, month);
        return globalLock?.IsLocked ?? isPast;
    }

    public async Task<PeriodLock> CreateAsync(PeriodLock periodLock)
    {
        db.PeriodLocks.Add(periodLock);
        await db.SaveChangesAsync();
        return periodLock;
    }

    public async Task<PeriodLock> UpdateAsync(PeriodLock periodLock)
    {
        db.PeriodLocks.Update(periodLock);
        periodLock.MarkModified(DateTime.UtcNow);
        await db.SaveChangesAsync();
        return periodLock;
    }
}
