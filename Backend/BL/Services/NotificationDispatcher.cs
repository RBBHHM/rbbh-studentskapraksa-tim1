using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>Only sends committed notices; never replays business grants or decisions.</summary>
public sealed class NotificationDispatcher(ConnectedPartiesDbContext db, IEmailService mail,
    IOptions<EmailSettings> settings, ILogger<NotificationDispatcher> logger)
{
    private static readonly SemaphoreSlim LocalClaimGate = new(1, 1);

    public async Task<int> DeliverBatchAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var ids = await db.NotificationDeliveries.AsNoTracking().Where(row => row.DeliveredAt == null && row.Attempts < 5 &&
                row.NextAttemptAt <= now && (row.LeaseUntil == null || row.LeaseUntil < now))
            .OrderBy(row => row.NextAttemptAt).ThenBy(row => row.Id).Select(row => row.Id).Take(50).ToListAsync(ct);
        var delivered = 0;
        foreach (var id in ids)
        {
            var lease = Guid.NewGuid();
            if (!await Claim(id, lease, ct)) continue;
            var row = await db.NotificationDeliveries.AsNoTracking().Include(row => row.Notification).SingleAsync(row => row.Id == id, ct);
            string? failure = null;
            try
            {
                var baseUrl = settings.Value.AppBaseUrl;
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
                    !(uri.Scheme == "https" || uri.Scheme == "http" && uri.IsLoopback) ||
                    uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0 ||
                    !row.Notification.Route.StartsWith("/app/", StringComparison.Ordinal))
                    throw new InvalidOperationException("Notification application URL is not configured safely.");
                // Semantic keys are the explicit fallback until the runtime catalog resolves them.
                // No request content is interpreted as HTML or used to construct destinations.
                var title = row.Notification.TitleKey;
                var text = row.Notification.BodyKey;
                var link = baseUrl.TrimEnd('/') + row.Notification.Route;
                var html = $"<p>{WebUtility.HtmlEncode(text)}</p><a href=\"{WebUtility.HtmlEncode(link)}\">{WebUtility.HtmlEncode(title)}</a>";
                await mail.SendApplicationNotificationAsync(row.Notification.RecipientEmail, title, html, ct);
                delivered++;
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                failure = "delivery.failed";
                logger.LogWarning("Notification delivery failed. FailureType {FailureType}", error.GetType().Name);
            }
            var completed = DateTime.UtcNow;
            if (db.Database.IsRelational())
                await db.NotificationDeliveries.Where(item => item.Id == id && item.LeaseId == lease).ExecuteUpdateAsync(update => update
                    .SetProperty(item => item.DeliveredAt, failure == null ? completed : (DateTime?)null)
                    .SetProperty(item => item.FailureCode, failure).SetProperty(item => item.LeaseId, (Guid?)null)
                    .SetProperty(item => item.LeaseUntil, (DateTime?)null)
                    .SetProperty(item => item.NextAttemptAt, completed.AddMinutes(Math.Min(60, Math.Pow(2, row.Attempts)))), ct);
            else
            {
                var tracked = await db.NotificationDeliveries.SingleAsync(item => item.Id == id && item.LeaseId == lease, ct);
                tracked.DeliveredAt = failure is null ? completed : null; tracked.FailureCode = failure;
                tracked.LeaseId = null; tracked.LeaseUntil = null; tracked.NextAttemptAt = completed.AddMinutes(Math.Min(60, Math.Pow(2, row.Attempts)));
                await db.SaveChangesAsync(ct);
            }
        }
        return delivered;
    }

    private async Task<bool> Claim(Guid id, Guid lease, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var query = db.NotificationDeliveries.Where(row => row.Id == id && row.DeliveredAt == null && row.Attempts < 5 &&
            row.NextAttemptAt <= now && (row.LeaseUntil == null || row.LeaseUntil < now));
        if (db.Database.IsRelational())
            return await query.ExecuteUpdateAsync(update => update.SetProperty(row => row.LeaseId, lease)
                .SetProperty(row => row.LeaseUntil, now.AddMinutes(2)).SetProperty(row => row.Attempts, row => row.Attempts + 1), ct) == 1;
        await LocalClaimGate.WaitAsync(ct);
        try
        {
            var row = await query.SingleOrDefaultAsync(ct);
            if (row is null) return false;
            row.LeaseId = lease; row.LeaseUntil = now.AddMinutes(2); row.Attempts++;
            await db.SaveChangesAsync(ct); return true;
        }
        finally { LocalClaimGate.Release(); }
    }
}
