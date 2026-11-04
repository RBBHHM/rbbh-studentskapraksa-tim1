using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

[ApiController, Route("api/notifications"), Authorize(Policy = ApplicationPolicies.ApplicationAccess)]
public sealed class NotificationsController(ConnectedPartiesDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<object> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] bool unreadOnly = false, CancellationToken ct = default)
    {
        var subject = AuthenticatedActor.RequireSubject(User);
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
        var query = db.Notifications.AsNoTracking().Where(row => row.RecipientSubject == subject);
        var unreadCount = await query.CountAsync(row => row.ReadAt == null, ct);
        if (unreadOnly) query = query.Where(row => row.ReadAt == null);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(row => row.CreatedAt).ThenBy(row => row.Id)
            .Skip((int)Math.Min((long)(page - 1) * pageSize, int.MaxValue)).Take(pageSize).ToListAsync(ct);
        return new { items = rows.Select(row => new { row.Id, row.TitleKey, row.BodyKey,
            parameters = JsonSerializer.Deserialize<JsonElement>(row.ParametersJson), row.Route, row.CreatedAt, row.ReadAt }), total, page, pageSize, unreadCount };
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var subject = AuthenticatedActor.RequireSubject(User);
        var row = await db.Notifications.SingleOrDefaultAsync(row => row.Id == id && row.RecipientSubject == subject, ct);
        if (row is null) return NotFound();
        row.ReadAt ??= DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
