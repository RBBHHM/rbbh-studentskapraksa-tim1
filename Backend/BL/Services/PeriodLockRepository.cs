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
            .Where(p => p.Year == year && p.Month == month && p.Department == null)
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

        // Historical Department values never override the global state.
        // Check global lock (Department = null)
        var globalLock = await GetByPeriodAsync(year, month);
        if (globalLock is not null)
            return globalLock.IsLocked;

        // Historical department records are not an authoritative global unlock.
        // Fail closed until an operator reconciles them to the canonical null row.
        return isPast || await db.PeriodLocks.AnyAsync(row => row.Year == year && row.Month == month && row.Department != null);
    }

    public async Task<PeriodLock> CreateAsync(PeriodLock periodLock)
    {
        periodLock.Department = null;
        if (await db.PeriodLocks.AnyAsync(row => row.Year == periodLock.Year && row.Month == periodLock.Month && row.Department != null))
            throw new ApplicationException("Historijsko stanje perioda zahtijeva administratorsko usklađivanje.");
        db.PeriodLocks.Add(periodLock);
        await db.SaveChangesAsync();
        return periodLock;
    }

    public async Task<PeriodLock> UpdateAsync(PeriodLock periodLock)
    {
        periodLock.Department = null;
        periodLock.ModifiedAt = DateTime.UtcNow;
        db.PeriodLocks.Update(periodLock);
        await db.SaveChangesAsync();
        return periodLock;
    }
}
