using RBBH.ConnectedParties.BL.Services;

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

            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }

            if (stoppingToken.IsCancellationRequested)
                break;

            try { await CheckAndLockCurrentPeriodAsync(stoppingToken); BackgroundOperationsMetrics.RecordSchedulerSuccess(); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error)
            {
                BackgroundOperationsMetrics.RecordSchedulerFailure();
                _logger.LogError("AutoLock check failed. FailureType {FailureType}", error.GetType().Name);
            }
        }
    }

    private async Task CheckAndLockCurrentPeriodAsync(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        if (!IsLastWorkingDayOfMonth(today)) return;
        await using var scope = _scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<PeriodWorkflow>().SetSystemLock(today.Year, today.Month, ct);
        _logger.LogInformation("AutoLock check completed for {Year}-{Month}", today.Year, today.Month);
    }

    private static TimeSpan GetDelayUntilNextEightOClock()
    {
        var now = DateTime.Now;
        var nextRun = now.Date.AddHours(8);

        if (now >= nextRun)
            nextRun = nextRun.AddDays(1);

        return nextRun - now;
    }

    private static bool IsLastWorkingDayOfMonth(DateTime date) =>
        date.Date == AutoLockHelper.GetLastWorkingDay(date.Year, date.Month).Date;
}
