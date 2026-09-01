using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;
using RBBH.ConnectedParties.DL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace RBBH.ConnectedParties.BL.Services;

public class PeriodLockRepository(ConnectedPartiesDbContext db) : IPeriodLockRepository
{
    public async Task<PeriodLock?> GetCurrentAsync(string? department = null)
    {
        var now = DateTime.UtcNow;
        return await GetByPeriodAsync(now.Year, now.Month, department);
    }

    public async Task<PeriodLock?> GetByPeriodAsync(int year, int month, string? department = null)
    {
        return await db.PeriodLocks
            .Where(p => p.Year == year && p.Month == month && p.Department == department)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsCurrentPeriodLockedAsync(string? department = null)
    {
        var now = DateTime.UtcNow;
        return await IsPeriodLockedAsync(now.Year, now.Month, department);
    }

    public async Task<bool> IsPeriodLockedAsync(int year, int month, string? department = null)
    {
        var now = DateTime.UtcNow;
        bool isPast = year < now.Year || (year == now.Year && month < now.Month);

        // Check department-specific lock
        if (department is not null)
        {
            var deptLock = await GetByPeriodAsync(year, month, department);
            if (deptLock is not null)
                return deptLock.IsLocked;
        }

        // Check global lock (Department = null)
        var globalLock = await GetByPeriodAsync(year, month, null);
        if (globalLock is not null)
            return globalLock.IsLocked;

        return isPast;
    }

    public async Task<PeriodLock> CreateAsync(PeriodLock periodLock)
    {
        db.PeriodLocks.Add(periodLock);
        await db.SaveChangesAsync();
        return periodLock;
    }

    public async Task<PeriodLock> UpdateAsync(PeriodLock periodLock)
    {
        periodLock.ModifiedAt = DateTime.UtcNow;
        db.PeriodLocks.Update(periodLock);
        await db.SaveChangesAsync();
        return periodLock;
    }
}
