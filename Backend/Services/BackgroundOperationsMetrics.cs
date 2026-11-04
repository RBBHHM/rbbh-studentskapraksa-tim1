using Microsoft.EntityFrameworkCore;
using Prometheus;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.Services;

public sealed record NotificationBacklog(int Pending, int Exhausted, double OldestPendingSeconds);

/// <summary>Aggregate diagnostics only. No recipients, subjects, payloads or request identifiers.</summary>
public static class BackgroundOperationsMetrics
{
    private static readonly Gauge Pending = Metrics.CreateGauge("rbbh_notification_pending", "Committed notices awaiting delivery, excluding exhausted attempts.");
    private static readonly Gauge Exhausted = Metrics.CreateGauge("rbbh_notification_exhausted", "Undelivered notices requiring operator review after the retry budget.");
    private static readonly Gauge Oldest = Metrics.CreateGauge("rbbh_notification_oldest_pending_seconds", "Age of the oldest pending committed notice.");
    private static readonly Gauge Observed = Metrics.CreateGauge("rbbh_notification_backlog_observed_timestamp_seconds", "Last successful backlog observation on this process.");
    private static readonly Gauge AutoLockSuccess = Metrics.CreateGauge("rbbh_period_scheduler_success_timestamp_seconds", "Last successful scheduled check, including dates without a scheduled lock.");
    private static readonly Counter AutoLockFailures = Metrics.CreateCounter("rbbh_period_scheduler_failures_total", "Failed scheduled checks on this process.");

    public static async Task<NotificationBacklog> ObserveBacklog(ConnectedPartiesDbContext db, CancellationToken ct)
    {
        var query = db.NotificationDeliveries.AsNoTracking().Where(row => row.DeliveredAt == null);
        var counts = await query.GroupBy(row => 1).Select(group => new
        { Pending = group.Count(row => row.Attempts < 5), Exhausted = group.Count(row => row.Attempts >= 5) }).SingleOrDefaultAsync(ct);
        var oldest = await query.Where(row => row.Attempts < 5).Select(row => (DateTime?)row.Notification.CreatedAt).MinAsync(ct);
        var snapshot = new NotificationBacklog(counts?.Pending ?? 0, counts?.Exhausted ?? 0,
            oldest is null ? 0 : Math.Max(0, (DateTime.UtcNow - oldest.Value).TotalSeconds));
        Pending.Set(snapshot.Pending); Exhausted.Set(snapshot.Exhausted); Oldest.Set(snapshot.OldestPendingSeconds);
        Observed.Set(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        return snapshot;
    }

    public static void RecordSchedulerSuccess() => AutoLockSuccess.Set(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    public static void RecordSchedulerFailure() => AutoLockFailures.Inc();
}
