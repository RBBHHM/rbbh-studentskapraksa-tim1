using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;

namespace RBBH.ConnectedParties.Services;

public class AutoLockHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutoLockHostedService> _logger;

    public AutoLockHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<AutoLockHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AutoLockHostedService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextEightOClock();

            _logger.LogInformation(
                "AutoLockHostedService waiting {Delay} until next 08:00 check.",
                delay
            );

            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            await CheckAndLockCurrentPeriodAsync();
        }
    }

    private async Task CheckAndLockCurrentPeriodAsync()
    {
        var today = DateTime.UtcNow.Date;
        var year = today.Year;
        var month = today.Month;

        if (!IsLastWorkingDayOfMonth(today))
        {
            _logger.LogInformation(
                "AutoLock: Today is not the last working day of the month. Date={Date}",
                today.ToString("yyyy-MM-dd")
            );

            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var periodLockRepository = scope.ServiceProvider.GetRequiredService<IPeriodLockRepository>();

        var currentPeriod = await periodLockRepository.GetCurrentAsync();

        if (currentPeriod != null && currentPeriod.IsLocked)
        {
            _logger.LogInformation(
                "AutoLock: Period {Month}/{Year} is already locked.",
                month,
                year
            );

            return;
        }

        if (currentPeriod == null)
        {
            var newPeriodLock = new PeriodLock
            {
                Year = year,
                Month = month,
                IsLocked = true,
                LockedBy = "system",
                LockedAt = DateTime.UtcNow,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await periodLockRepository.CreateAsync(newPeriodLock);
        }
        else
        {
            currentPeriod.IsLocked = true;
            currentPeriod.LockedBy = "system";
            currentPeriod.LockedAt = DateTime.UtcNow;
            currentPeriod.UnlockedBy = null;
            currentPeriod.UnlockedAt = null;
            currentPeriod.ModifiedBy = "system";
            currentPeriod.ModifiedAt = DateTime.UtcNow;

            await periodLockRepository.UpdateAsync(currentPeriod);
        }

        _logger.LogInformation(
            "AutoLock: Period {Month}/{Year} automatski zaključan.",
            month,
            year
        );
    }

    private static TimeSpan GetDelayUntilNextEightOClock()
    {
        var now = DateTime.Now;
        var nextRun = now.Date.AddHours(8);

        if (now >= nextRun)
            nextRun = nextRun.AddDays(1);

        return nextRun - now;
    }

    private static bool IsLastWorkingDayOfMonth(DateTime date)
    {
        var lastDay = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));

        while (lastDay.DayOfWeek == DayOfWeek.Saturday ||
               lastDay.DayOfWeek == DayOfWeek.Sunday)
        {
            lastDay = lastDay.AddDays(-1);
        }

        return date.Date == lastDay.Date;
    }
}