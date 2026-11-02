using RBBH.ConnectedParties.BL.Services;

namespace RBBH.ConnectedParties.Services;

public sealed class NotificationDeliveryWorker(IServiceScopeFactory scopes, ILogger<NotificationDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<NotificationDispatcher>().DeliverBatchAsync(stoppingToken);
                }
                catch (Exception error) when (error is not OperationCanceledException)
                { logger.LogWarning("Notification batch failed. FailureType {FailureType}", error.GetType().Name); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
