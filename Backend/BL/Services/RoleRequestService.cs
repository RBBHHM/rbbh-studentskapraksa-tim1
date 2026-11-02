using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.DTO.Roles;
using RBBH.ConnectedParties.DL.Entities.Authorization;
using RBBH.ConnectedParties.DL.Entities.Audit;
using RBBH.ConnectedParties.DL.Entities.Notifications;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions;
using RBBH.ConnectedParties.Helpers;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.BL.Services;

public sealed class RoleRequestService(ConnectedPartiesDbContext db, IHttpContextAccessor accessor)
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User ?? throw new MissingActorIdentityException();
    private string Subject => AuthenticatedActor.RequireSubject(Principal);

    public async Task<object> Options(CancellationToken ct) => new
    {
        items = await db.Roles.AsNoTracking().Where(role => role.IsActive && role.Name != ApplicationAccessRoles.Guest)
            .OrderBy(role => role.Name).Select(role => new { role.Id, role.Name }).ToListAsync(ct)
    };

    public Task<(RoleRequestDTO Request, bool Created)> Submit(CreateRoleRequestDTO input, CancellationToken ct)
    {
        var subject = Subject;
        var email = AuthenticatedActor.Email(Principal).ToLowerInvariant();
        var verified = Principal.FindAll("email_verified").Any(claim => bool.TryParse(claim.Value, out var value) && value);
        var username = Principal.FindFirst("preferred_username")?.Value ?? subject;
        var first = Principal.FindFirst("given_name")?.Value ?? "";
        var last = Principal.FindFirst("family_name")?.Value ?? "";
        if (!verified || !MailAddress.TryCreate(email, out var address) || address.Address != email)
            throw new BusinessRuleException(403, "identity.verified_email_required", "Potrebna je potvrđena email adresa.");
        if (username.Length > 100 || first.Length > 100 || last.Length > 100 || email.Length > 200 ||
            input.ClientRequestId == Guid.Empty || input.Purpose is not ("new" or "change") ||
            input.RoleIds.Length > 20 || input.RoleIds.Distinct().Count() != input.RoleIds.Length ||
            (input.Purpose == "new" && input.RoleIds.Length == 0) || input.Note?.Length > 1000)
            throw new BusinessRuleException(400, "role_request.invalid", "Provjerite podatke zahtjeva i odabrane role.");

        return Locked(async () =>
        {
            await EnsureActiveApplicant(subject, email, ct);
            var prior = await Query().SingleOrDefaultAsync(row => row.RequesterSubject == subject && row.ClientRequestId == input.ClientRequestId, ct);
            if (prior is not null)
            {
                if (prior.Purpose != input.Purpose || prior.Note != input.Note?.Trim() ||
                    !prior.RequestedRoles.Select(item => item.RoleId).Order().SequenceEqual(input.RoleIds.Order()))
                    throw Conflict("role_request.idempotency_mismatch");
                return (Map(prior, false), false);
            }
            if (await db.RoleRequests.AnyAsync(row => row.RequesterSubject == subject && row.Status == "pending", ct))
                throw Conflict("role_request.pending_exists");
            var roles = await db.Roles.Where(role => input.RoleIds.Contains(role.Id) && role.IsActive && role.Name != ApplicationAccessRoles.Guest).ToListAsync(ct);
            if (roles.Count != input.RoleIds.Length) throw InvalidRoles();
            var admins = await ActiveAdmins().ToListAsync(ct);
            if (admins.Count == 0) throw new BusinessRuleException(503, "role_request.approver_unavailable", "Obrada zahtjeva trenutno nije dostupna.");
            var row = new RoleRequest { ClientRequestId = input.ClientRequestId, RequesterSubject = subject,
                Username = username, FirstName = first, LastName = last, Email = email, Purpose = input.Purpose, Note = input.Note?.Trim() };
            row.RequestedRoles = roles.Select(role => new RequestedRole { Role = role, RoleId = role.Id, RequestId = row.Id }).ToList();
            db.RoleRequests.Add(row);
            db.AuditLogs.Add(Audit(row, "ROLE_REQUEST", null, new { row.Purpose, Roles = roles.Select(role => role.Name) }));
            foreach (var admin in admins)
                Notify(admin.KeycloakId, admin.Email, $"role-request:{row.Id}:submitted", "role_request.submitted", row, "/app/admin/role-requests");
            await db.SaveChangesAsync(ct);
            return (Map(row, false), true);
        }, ct);
    }

    public async Task<PagedResponse<RoleRequestDTO>> List(bool review, string? status, int page, int pageSize, CancellationToken ct)
    {
        if (review) await EnsureAdmin(ct);
        var query = Query().AsNoTracking();
        if (!review) { var subject = Subject; query = query.Where(row => row.RequesterSubject == subject); }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(row => row.Status == status);
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(row => row.CreatedAt).ThenBy(row => row.Id)
            .Skip((int)Math.Min((long)(page - 1) * pageSize, int.MaxValue)).Take(pageSize).ToListAsync(ct);
        return new(rows.Select(row => Map(row, review)).ToArray(), total, page, pageSize);
    }

    public Task<RoleRequestDTO> Decide(Guid id, bool approve, RoleRequestDecisionDTO input, CancellationToken ct)
    {
        if (input.DecisionId == Guid.Empty || input.ExpectedVersion < 1 || input.Note?.Length > 1000)
            throw new BusinessRuleException(400, "role_request.invalid_decision", "Provjerite podatke odluke.");
        return Locked(async () =>
        {
            await EnsureAdmin(ct); // Recheck current database grants inside the user-mutation lock.
            var row = await Query().SingleOrDefaultAsync(row => row.Id == id, ct)
                ?? throw new BusinessRuleException(404, "request.not_found", "Zahtjev nije pronađen.");
            if (row.RequesterSubject == Subject) throw new BusinessRuleException(403, "role_request.self_decision_denied", "Nije dozvoljena obrada vlastitog zahtjeva.");
            var outcome = approve ? "approved" : "rejected";
            if (row.Status != "pending")
            {
                if (row.DecisionId == input.DecisionId && row.Status == outcome && row.ProcessedBy == Subject && row.DecisionNote == input.Note?.Trim()) return Map(row, true);
                throw Conflict("role_request.already_processed");
            }
            if (row.Version != input.ExpectedVersion) throw Conflict("request.concurrent_change");
            object? oldRoles = null;
            if (approve)
            {
                await EnsureActiveApplicant(row.RequesterSubject, row.Email, ct);
                if (row.RequestedRoles.Any(item => !item.Role.IsActive || item.Role.Name == ApplicationAccessRoles.Guest)) throw InvalidRoles();
                var user = await db.AppUsers.SingleOrDefaultAsync(user => user.KeycloakSubjectConfirmed && user.KeycloakId == row.RequesterSubject, ct);
                if (user is null)
                {
                    // Never take over a confirmed/legacy account by matching email or username.
                    var email = row.Email.ToUpperInvariant(); var name = row.Username.ToUpperInvariant();
                    if (await db.AppUsers.AnyAsync(user => user.KeycloakId == row.RequesterSubject || user.Email.Trim().ToUpper() == email || user.Username.Trim().ToUpper() == name, ct))
                        throw Conflict("identity.reconciliation_required");
                    user = new AppUser { KeycloakId = row.RequesterSubject, KeycloakSubjectConfirmed = true,
                        KeycloakSubjectLinkedAt = DateTime.UtcNow, Username = row.Username, FirstName = row.FirstName,
                        LastName = row.LastName, Email = row.Email, CreatedBy = AuthenticatedActor.Email(Principal) };
                    db.AppUsers.Add(user);
                }
                var existing = await db.UserRoles.Where(item => item.UserId == user.Id).Include(item => item.Role).ToListAsync(ct);
                oldRoles = existing.Where(item => item.IsActive).Select(item => item.Role.Name).ToArray();
                var targets = row.RequestedRoles.Select(item => item.RoleId).ToHashSet();
                if (row.Purpose == "new") targets.UnionWith(existing.Where(item => item.IsActive).Select(item => item.RoleId));
                var removesAdmin = existing.Any(item => item.IsActive && item.Role.Name == ApplicationAccessRoles.Administrator && !targets.Contains(item.RoleId));
                if (removesAdmin && await ActiveAdmins().Select(user => user.Id).Distinct().Take(2).CountAsync(ct) <= 1)
                    throw Conflict("users.final_admin");
                foreach (var assignment in existing) assignment.IsActive = targets.Contains(assignment.RoleId);
                foreach (var roleId in targets.Except(existing.Select(item => item.RoleId)))
                    db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId, CreatedBy = AuthenticatedActor.Email(Principal) });
                user.ModifiedAt = DateTime.UtcNow; user.ModifiedBy = AuthenticatedActor.Email(Principal);
            }
            row.Status = outcome; row.DecisionId = input.DecisionId; row.DecisionNote = input.Note?.Trim();
            row.ProcessedAt = DateTime.UtcNow; row.ProcessedBy = Subject; row.Version++;
            db.AuditLogs.Add(Audit(row, approve ? "ROLE_REQUEST_APPROVED" : "ROLE_REQUEST_REJECTED", oldRoles,
                new { row.Status, Roles = row.RequestedRoles.Select(item => item.Role.Name) }));
            Notify(row.RequesterSubject, row.Email, $"role-request:{row.Id}:{outcome}", "role_request." + outcome, row, "/app/profile");
            await db.SaveChangesAsync(ct); // Request, grants, audit, inbox and outbox share one commit.
            return Map(row, true);
        }, ct);
    }

    private IQueryable<RoleRequest> Query() => db.RoleRequests.Include(row => row.RequestedRoles).ThenInclude(item => item.Role);
    private IQueryable<AppUser> ActiveAdmins() => db.AppUsers.Where(user => user.IsActive && user.KeycloakSubjectConfirmed &&
        db.UserRoles.Any(assignment => assignment.UserId == user.Id && assignment.IsActive && assignment.Role.IsActive && assignment.Role.Name == ApplicationAccessRoles.Administrator &&
            assignment.Role.RolePermissions.Any(grant => grant.Permission.IsActive && grant.Permission.Code == ApplicationPermissions.AdministrationManage)));
    private async Task EnsureAdmin(CancellationToken ct)
    {
        var subject = Subject;
        if (!await ActiveAdmins().AnyAsync(user => user.KeycloakId == subject, ct))
            throw new BusinessRuleException(403, "authorization.admin_required", "Za ovu radnju su potrebne administratorske dozvole.");
    }
    private async Task EnsureActiveApplicant(string subject, string email, CancellationToken ct)
    {
        var normalized = email.ToUpperInvariant();
        if (await db.AppUsers.AnyAsync(user => !user.IsActive && (user.KeycloakId == subject || user.Email.Trim().ToUpper() == normalized), ct))
            throw new BusinessRuleException(403, "identity.inactive", "Pristup trenutno nije dozvoljen.");
    }
    private Task<T> Locked<T>(Func<Task<T>> mutation, CancellationToken ct) => UsersMutationBoundary.ExecuteAsync(db, mutation,
        () => throw Conflict("request.busy"), ct);
    private static BusinessRuleException Conflict(string code) => new(409, code, "Zahtjev je promijenjen ili zahtijeva provjeru. Osvježite podatke.");
    private static BusinessRuleException InvalidRoles() => new(400, "role_request.invalid_roles", "Odabrane role nisu dostupne.");
    private static RoleRequestDTO Map(RoleRequest row, bool review) => new(row.Id, row.Purpose, row.Status,
        row.RequestedRoles.Select(item => item.RoleId).Order().ToArray(), row.RequestedRoles.Select(item => item.Role.Name).Order().ToArray(),
        row.Note, row.CreatedAt, row.ProcessedAt, row.ProcessedBy, row.DecisionNote, row.Version,
        review ? row.Username : null, review ? row.Email : null);
    private AuditLog Audit(RoleRequest row, string action, object? before, object after) => new()
    {
        TableName = "RoleRequest", RecordId = row.Id.ToString(), Action = action, UserId = Subject,
        Username = Principal.FindFirst("preferred_username")?.Value ?? Subject,
        IpAddress = accessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
        OldValues = before is null ? null : JsonSerializer.Serialize(before), NewValues = JsonSerializer.Serialize(after)
    };
    private void Notify(string recipient, string email, string eventKey, string messageKey, RoleRequest row, string route)
    {
        var notice = new ApplicationNotification { RecipientSubject = recipient, RecipientEmail = email, EventKey = eventKey,
            TitleKey = "notifications." + messageKey + ".title", BodyKey = "notifications." + messageKey + ".body",
            ParametersJson = JsonSerializer.Serialize(new { requester = row.Username, requestId = row.Id, status = row.Status }), Route = route };
        notice.Delivery = new NotificationDelivery { NotificationId = notice.Id };
        db.Notifications.Add(notice);
    }
}
