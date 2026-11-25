using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Entities.Notifications;
using RBBH.ConnectedParties.DL.Entities.PeriodLock;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions;
using RBBH.ConnectedParties.Helpers;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>One commit for period/request state, audit, recipient inbox and delivery outbox.</summary>
public sealed class PeriodWorkflow(ConnectedPartiesDbContext db)
{
    private sealed record Actor(string Subject, string Username, string Email);
    private static Actor Capture(ClaimsPrincipal principal) => new(AuthenticatedActor.RequireSubject(principal),
        AuthenticatedActor.Username(principal), AuthenticatedActor.Email(principal));
    private static void Period(int year, int month)
    { if (year is < 2020 or > 9999 || month is < 1 or > 12) throw new BusinessRuleException(400, "period.invalid"); }
    private static void Text(string? value, int minimum, int maximum, string code)
    { if (string.IsNullOrWhiteSpace(value) || value.Length > maximum || value.Trim().Length < minimum) throw new BusinessRuleException(400, code, new object?[] { minimum, maximum }); }
    private static bool Mailbox(string? email) => email is { Length: > 0 and <= 200 } && MailAddress.TryCreate(email, out var address) && address.Address == email;
    private void Audit(string id, string action, Actor actor, object? before, object after) => db.AuditLogs.Add(new()
    { TableName = "PeriodLock", RecordId = id, Action = action, Username = actor.Username,
        Timestamp = DateTime.UtcNow, OldValues = before is null ? null : JsonSerializer.Serialize(before), NewValues = JsonSerializer.Serialize(after) });
    private void Notice(string? subject, string email, string eventKey, string kind, object parameters, string route = "/app/admin/period")
    {
        var notice = new ApplicationNotification { RecipientSubject = subject ?? "", RecipientEmail = email, EventKey = eventKey,
            TitleKey = "notifications.period." + kind + ".title", BodyKey = "notifications.period." + kind + ".body",
            ParametersJson = JsonSerializer.Serialize(parameters), Route = route };
        if (Mailbox(email)) notice.Delivery = new NotificationDelivery { NotificationId = notice.Id };
        db.Notifications.Add(notice);
    }
    private async Task NotifyReviewers(string eventKey, string kind, object parameters, CancellationToken ct)
    {
        var reviewers = await db.AppUsers.AsNoTracking()
            .Where(user => user.IsActive && user.KeycloakSubjectConfirmed && user.KeycloakId != "" &&
                db.UserRoles.Any(role => role.UserId == user.Id && role.IsActive && role.Role.IsActive && role.Role.RolePermissions.Any(grant => grant.Permission.IsActive &&
                    (grant.Permission.Code == ApplicationPermissions.CapitalCreate || grant.Permission.Code == ApplicationPermissions.CapitalEdit || grant.Permission.Code == ApplicationPermissions.CapitalDelete))))
            .Select(user => new { user.KeycloakId, user.Email }).ToListAsync(ct);
        if (reviewers.Count == 0) throw new BusinessRuleException(503, "period.notification_setup");
        foreach (var reviewer in reviewers)
            Notice(reviewer.KeycloakId, reviewer.Email, eventKey, kind, parameters);
    }
    private async Task Owner(UnlockRequest request, string eventKey, string kind, object parameters, CancellationToken ct)
    {
        var email = request.RequestedByEmail;
        if (request.RequestedBySubject is not null)
            email = await db.AppUsers.Where(user => user.KeycloakId == request.RequestedBySubject && user.KeycloakSubjectConfirmed).Select(user => user.Email).SingleOrDefaultAsync(ct) ?? email;
        Notice(request.RequestedBySubject, email, eventKey, kind, parameters);
    }
    public Task SetLock(int year, int month, bool locked, ClaimsPrincipal principal, CancellationToken ct) =>
        SetLock(year, month, locked, Capture(principal), false, ct);
    public Task SetSystemLock(int year, int month, CancellationToken ct) => SetLock(year, month, true, new("system", "system", ""), true, ct);
    private async Task SetLock(int year, int month, bool locked, Actor actor, bool system, CancellationToken ct)
    {
        Period(year, month); var now = DateTime.UtcNow;
        var row = await db.PeriodLocks.IgnoreQueryFilters().SingleOrDefaultAsync(row => row.Year == year && row.Month == month, ct);
        if (row is { IsActive: false })
            throw new BusinessRuleException(409, "period.reconciliation_required");
        if (locked && row?.IsLocked == true)
        { if (system) return; throw new BusinessRuleException(400, "period.already_locked"); }
        var past = year < now.Year || year == now.Year && month < now.Month;
        if (!locked && row?.IsLocked != true && !past) throw new BusinessRuleException(400, "period.not_locked");
        var pending = !locked ? await db.UnlockRequests.Where(request => request.Year == year && request.Month == month && request.Status == "PENDING").ToListAsync(ct) : [];
        if (!locked && row is { IsLocked: false } && pending.Count == 0) return; // No false repeat audit/broadcast.
        bool? wasLocked = row?.IsLocked;
        if (row is null) { row = new() { Year = year, Month = month, CreatedBy = actor.Username, CreatedAt = now }; db.PeriodLocks.Add(row); }
        row.IsLocked = locked; row.MarkModified(now); row.ModifiedBy = actor.Username;
        if (locked) { row.LockedBy = actor.Username; row.LockedAt = now; row.UnlockedAt = null; row.UnlockedBy = null; }
        else { row.UnlockedBy = actor.Username; row.UnlockedAt = now; }
        foreach (var request in pending)
        {
            request.Status = "APPROVED"; request.ProcessedAt = now; request.ProcessedBy = actor.Username; request.MarkModified(now); request.ModifiedBy = actor.Username;
        }
        Audit($"{year}-{month:D2}", locked ? "PERIOD_LOCK" : "PERIOD_UNLOCK", actor, new { isLocked = wasLocked }, new { isLocked = locked, year, month, approvedRequestIds = pending.Select(request => request.Id) });
        var eventKey = $"period:{year}-{month:D2}:{Guid.NewGuid():N}";
        var recipients = await db.AppUsers.Where(user => user.IsActive && user.KeycloakSubjectConfirmed && user.KeycloakId != "")
            .Select(user => new { user.KeycloakId, user.Email }).ToListAsync(ct);
        foreach (var recipient in recipients)
            Notice(recipient.KeycloakId, recipient.Email, eventKey, locked ? "locked" : "unlocked", new { year, month, actor = actor.Username });
        // No 200-user API clamp: every known active/confirmed recipient is staged in this commit.
        await db.SaveChangesAsync(ct);
    }
    public async Task Request(int year, int month, string reason, ClaimsPrincipal principal, CancellationToken ct)
    {
        Period(year, month); Text(reason, 10, 500, "period.invalid_reason"); var actor = Capture(principal);
        var request = new UnlockRequest { Year = year, Month = month, Reason = reason, RequestedBySubject = actor.Subject,
            RequestedBy = actor.Username, RequestedByEmail = actor.Email, Status = "PENDING", CreatedBy = actor.Username, CreatedAt = DateTime.UtcNow };
        db.UnlockRequests.Add(request);
        Audit($"{year}-{month:D2}", "UNLOCK_REQUEST", actor, null, new { request.Id, year, month, requestedBy = actor.Username, reason });
        await NotifyReviewers($"period-request:{request.Id}:submitted", "requested", new { year, month, requester = actor.Username, message = reason }, ct);
        await db.SaveChangesAsync(ct);
    }
    public async Task Process(Guid id, bool reject, string note, ClaimsPrincipal principal, CancellationToken ct)
    {
        Text(note, 10, 1000, "period.invalid_note"); var actor = Capture(principal);
        var request = await db.UnlockRequests.SingleOrDefaultAsync(row => row.Id == id && row.Status == "PENDING", ct)
            ?? throw new BusinessRuleException(404, "period.request_not_pending");
        var before = new { request.Status, request.AdminNote, request.ProcessedBy, request.ProcessedAt };
        request.Status = reject ? "REJECTED" : "NEEDS_INFO"; request.AdminNote = note.Trim(); request.ProcessedBy = actor.Username;
        var now = DateTime.UtcNow; request.ProcessedAt = now; request.MarkModified(now); request.ModifiedBy = actor.Username;
        Audit(id.ToString(), reject ? "UNLOCK_REQUEST_REJECTED" : "UNLOCK_REQUEST_NEEDS_INFO", actor, before, new { request.Status, request.AdminNote, request.ProcessedBy, request.ProcessedAt });
        await Owner(request, $"period-request:{id}:{request.Status}", reject ? "rejected" : "needsinfo", new { year = request.Year, month = request.Month, message = note, actor = actor.Username }, ct);
        await db.SaveChangesAsync(ct);
    }
    public async Task Respond(Guid id, string message, ClaimsPrincipal principal, CancellationToken ct)
    {
        Text(message, 10, 490, "period.invalid_response"); var actor = Capture(principal);
        var original = await db.UnlockRequests.SingleOrDefaultAsync(row => row.Id == id && row.RequestedBySubject == actor.Subject, ct)
            ?? throw new BusinessRuleException(404, "period.request_not_found");
        if (original.Status != "NEEDS_INFO" || await db.UnlockRequests.AnyAsync(row => row.ParentRequestId == id, ct))
            throw new BusinessRuleException(409, "period.request_invalid_state");
        var now = DateTime.UtcNow;
        var response = new UnlockRequest { ParentRequestId = id, RequestedBySubject = actor.Subject, RequestedBy = actor.Username,
            RequestedByEmail = actor.Email, Year = original.Year, Month = original.Month, Reason = "[ODGOVOR] " + message.Trim(),
            Status = "PENDING", CreatedBy = actor.Username, CreatedAt = now };
        original.MarkModified(now); original.ModifiedBy = actor.Username; db.UnlockRequests.Add(response);
        Audit(id.ToString(), "UNLOCK_REQUEST", actor, null, new { type = "user_response", originalRequestId = id, responseId = response.Id, year = original.Year, month = original.Month });
        await NotifyReviewers($"period-request:{response.Id}:response", "response", new { year = original.Year, month = original.Month, requester = actor.Username, message = message.Trim() }, ct);
        await db.SaveChangesAsync(ct);
    }
}
