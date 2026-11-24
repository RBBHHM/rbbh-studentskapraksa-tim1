using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>Only sends committed notices; never replays business grants or decisions.</summary>
public sealed class NotificationDispatcher(ConnectedPartiesDbContext db, IEmailService mail,
    IConfiguration configuration, ILogger<NotificationDispatcher> logger, RuntimeLocalizationCatalog localization)
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
                var titleKey = row.Notification.TitleKey.Replace('_', '.');
                var bodyKey = row.Notification.BodyKey.Replace('_', '.');
                var title = await localization.Text(titleKey, null, ct);
                var text = await localization.Text(bodyKey, null, ct);
                // Missing setup/templates is retryable; sending raw keys would falsely mark delivery as successful.
                if (title == titleKey || text == bodyKey) throw new InvalidOperationException("Notification resources unavailable.");
                var roleStatus = titleKey switch
                {
                    "notifications.role.request.submitted.title" => "submitted",
                    "notifications.role.request.approved.title" => "approved",
                    "notifications.role.request.rejected.title" => "rejected",
                    _ => null
                };
                var parametersJson = row.Notification.ParametersJson;
                if (parametersJson.Length > 32000) throw new InvalidOperationException("Notification parameters too large.");
                using var parameters = JsonDocument.Parse(parametersJson, new JsonDocumentOptions { MaxDepth = 8 });
                var values = parameters.RootElement;
                if (values.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Invalid notification parameters.");
                var route = row.Notification.Route;
                if (roleStatus is not null)
                {
                    var noteKey = roleStatus == "submitted" ? "note" : "decisionNote";
                    if (values.TryGetProperty(noteKey, out var note) && note.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(note.GetString()))
                        text = string.Join('\n', text.Split('\n').Where(line => !line.Contains("{" + noteKey + "}", StringComparison.Ordinal)));
                    if (values.TryGetProperty("requestId", out var idValue) && idValue.ValueKind == JsonValueKind.String && idValue.TryGetGuid(out var requestId))
                        route = (roleStatus == "submitted" ? "/app/admin/role-requests" : "/app/profile") + $"?requestId={requestId:D}";
                }
                title = Render(title, values);
                text = Render(text, values);
                if (title.Length > 500 || title.Any(character => character is '\r' or '\n')) throw new InvalidOperationException("Invalid notification title.");
                var encodedText = WebUtility.HtmlEncode(text).Replace("\r\n", "\n").Replace("\n", "<br>");
                var origin = ApplicationOrigin();
                var link = origin is not null && route.StartsWith("/app/", StringComparison.Ordinal) ? origin + route : null;
                var html = roleStatus is not null
                    ? RoleRequestMail.Build(roleStatus, title, encodedText, link,
                        origin, localization.DefaultLanguage)
                    : $"<p>{encodedText}</p>" + (link is null ? "" : $"<a href=\"{WebUtility.HtmlEncode(link)}\">{WebUtility.HtmlEncode(title)}</a>");
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

    private string? ApplicationOrigin()
    {
        var baseUrl = configuration["Application:PublicUrl"];
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            !(uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback) ||
            uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            return null;

        return uri.AbsoluteUri.TrimEnd('/');
    }

    private static string Render(string template, JsonElement values)
    {
        var result = Regex.Replace(template, @"\{([A-Za-z][A-Za-z0-9]*)\}", match =>
        {
            if (!values.TryGetProperty(match.Groups[1].Value, out var value)) throw new InvalidOperationException("Notification parameter missing.");
            return value.ValueKind switch { JsonValueKind.String => value.GetString() ?? "", JsonValueKind.Null => "",
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(), _ => throw new InvalidOperationException("Invalid notification parameter value.") };
        });
        if (result.Length > 32000) throw new InvalidOperationException("Rendered notification too large.");
        return result;
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
