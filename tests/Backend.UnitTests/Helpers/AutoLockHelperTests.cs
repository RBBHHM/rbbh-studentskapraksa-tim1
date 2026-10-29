using RBBH.ConnectedParties.Services;

namespace UnitTests.Helpers
{
    /// <summary>
    /// EC-7 — Unit testovi za logiku zadnjeg radnog dana.
    ///
    /// Testira AutoLockHelper.GetLastWorkingDay(year, month) — helper klasa
    /// izvučena iz privatne logike u AutoLockHostedService radi testabilnosti.
    ///
    /// Ovi testovi NE zahtijevaju pokrenuti backend niti Docker/SQL Server.
    /// </summary>
    public class AutoLockHelperTests
    {
        // ─────────────────────────────────────────────────────────────
        // EC-7 — Parametrizovani test: nikad ne vraća vikend
        // ─────────────────────────────────────────────────────────────

        [Theory]
        [InlineData(2026, 1)]  // Januar 2026 — zadnji dan subota (31.01) → petak 30.01
        [InlineData(2026, 5)]  // Maj 2026 — zadnji dan nedjelja (31.05) → petak 29.05
        [InlineData(2026, 4)]  // April 2026 — zadnji dan četvrtak (30.04) → četvrtak 30.04
        public void GetLastWorkingDay_NeverReturnsWeekend(int year, int month)
        {
            var result = AutoLockHelper.GetLastWorkingDay(year, month);

            Assert.NotEqual(DayOfWeek.Saturday, result.DayOfWeek);
            Assert.NotEqual(DayOfWeek.Sunday, result.DayOfWeek);
            Assert.Equal(year, result.Year);
            Assert.Equal(month, result.Month);
        }

        // Januar 2026: 31.01.2026 je subota → vraća petak 30.01.2026
        [Fact]
        public void GetLastWorkingDay_WhenLastDayIsSaturday_ReturnsFriday()
        {
            var result = AutoLockHelper.GetLastWorkingDay(2026, 1);

            Assert.Equal(new DateTime(2026, 1, 30), result.Date);
            Assert.Equal(DayOfWeek.Friday, result.DayOfWeek);
        }

        // Maj 2026: 31.05.2026 je nedjelja → vraća petak 29.05.2026
        [Fact]
        public void GetLastWorkingDay_WhenLastDayIsSunday_ReturnsFriday()
        {
            var result = AutoLockHelper.GetLastWorkingDay(2026, 5);

            Assert.Equal(new DateTime(2026, 5, 29), result.Date);
            Assert.Equal(DayOfWeek.Friday, result.DayOfWeek);
        }

        // April 2026: 30.04.2026 je četvrtak → vraća četvrtak 30.04.2026
        [Fact]
        public void GetLastWorkingDay_WhenLastDayIsWeekday_ReturnsSameDay()
        {
            var result = AutoLockHelper.GetLastWorkingDay(2026, 4);

            Assert.Equal(new DateTime(2026, 4, 30), result.Date);
            Assert.Equal(DayOfWeek.Thursday, result.DayOfWeek);
        }
    }
}
