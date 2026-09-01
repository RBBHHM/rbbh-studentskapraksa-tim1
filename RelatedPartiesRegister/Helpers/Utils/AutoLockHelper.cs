namespace RBBH.ConnectedParties.Services;

/// <summary>
/// Helper za logiku automatskog zaključavanja perioda.
/// Izvučeno iz AutoLockHostedService radi testabilnosti (EC-7).
/// </summary>
public static class AutoLockHelper
{
    /// <summary>
    /// Vraća zadnji radni dan (ponedjeljak–petak) u datom mjesecu.
    /// Ako zadnji dan pada na vikend, ide unazad dok ne nađe radni dan.
    /// </summary>
    public static DateTime GetLastWorkingDay(int year, int month)
    {
        var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));

        while (lastDay.DayOfWeek == DayOfWeek.Saturday ||
               lastDay.DayOfWeek == DayOfWeek.Sunday)
        {
            lastDay = lastDay.AddDays(-1);
        }

        return lastDay;
    }
}