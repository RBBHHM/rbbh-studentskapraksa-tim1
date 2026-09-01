using RBBH.ConnectedParties.DL.Entities.PeriodLock;

namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

public interface IPeriodLockRepository
{
    /// <summary>Vraća PeriodLock za trenutni mjesec i dati odjel (null = globalni).</summary>
    Task<PeriodLock?> GetCurrentAsync(string? department = null);

    /// <summary>Vraća PeriodLock za određeni period i odjel.</summary>
    Task<PeriodLock?> GetByPeriodAsync(int year, int month, string? department = null);

    /// <summary>Vraća true ako je trenutni period zaključan za dati odjel.</summary>
    Task<bool> IsCurrentPeriodLockedAsync(string? department = null);

    /// <summary>
    /// Vraća true ako je dati period zaključan za dati odjel.
    /// Provjerava i department-specifičan lock i globalni lock.
    /// </summary>
    Task<bool> IsPeriodLockedAsync(int year, int month, string? department = null);

    Task<PeriodLock> CreateAsync(PeriodLock periodLock);
    Task<PeriodLock> UpdateAsync(PeriodLock periodLock);
}
