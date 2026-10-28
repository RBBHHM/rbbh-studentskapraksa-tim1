using System;
using System.Globalization;
using RBBH.ConnectedParties.BL.ServiceInterfaces;

namespace RBBH.ConnectedParties.BL.Services;

public class PeriodLockService : IPeriodLockService
{
    public string GetMonthDisplayName(int year, int month)
    {
        try
        {
            // Postavlja jezik na bosanski (latinica, BiH) prema zahtjevu banke
            var culture = new CultureInfo("bs-Latn-BA");
            var dateTime = new DateTime(year, month, 1);
            return $"{culture.TextInfo.ToTitleCase(dateTime.ToString("MMMM", culture))} {year}.";
        }
        catch
        {
            return $"{month}/{year}";
        }
    }
}