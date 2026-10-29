using RBBH.ConnectedParties.DL.Entities.PeriodLock;

namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

public interface IPeriodLockRepository
{
    /// Global period state, independent of usernames.
    Task<PeriodLock?> GetCurrentAsync();

    /// Global period state, independent of usernames.
    Task<PeriodLock?> GetByPeriodAsync(int year, int month);

    /// Global period state, independent of usernames.
    Task<bool> IsCurrentPeriodLockedAsync();

    /// <summary>
    /// Global period state, independent of usernames.
    /// Historical department rows require operator reconciliation, never runtime overrides.
    /// </summary>
    Task<bool> IsPeriodLockedAsync(int year, int month);

    Task<PeriodLock> CreateAsync(PeriodLock periodLock);
    Task<PeriodLock> UpdateAsync(PeriodLock periodLock);
}
