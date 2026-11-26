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

public sealed record RoleRequestResult(RoleRequestDTO? Request, int Status, string? Code = null)
{
    public static RoleRequestResult Failure(int status, string code) => new(null, status, code);
}

public sealed class RoleRequestService(ConnectedPartiesDbContext db, IHttpContextAccessor accessor)
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User ?? throw new MissingActorIdentityException();
    private string Subject => AuthenticatedActor.RequireSubject(Principal);

    public async Task<object> Options(CancellationToken ct)
    {
        // Existing databases can use different SQL Server collations for Name and DisplayName.
        var roles = await db.Roles.AsNoTracking()
            .Where(role => role.IsActive && role.Name != ApplicationAccessRoles.Guest)
            .OrderBy(role => role.Name)
            .Select(role => new { role.Id, role.Name, role.DisplayName })
            .ToListAsync(ct);

        return new { items = roles.Select(role => new { role.Id, role.Name, displayName = role.DisplayName ?? role.Name }).ToArray() };
    }

    public async Task<RoleRequestResult> Submit(CreateRoleRequestDTO input, CancellationToken ct)
    {
        var subject = Subject;
        var verified = Principal.FindAll("email_verified").Any(claim => bool.TryParse(claim.Value, out var value) && value);
        var username = Principal.FindFirst("preferred_username")?.Value ?? subject;
        var first = Principal.FindFirst("given_name")?.Value ?? "";
        var last = Principal.FindFirst("family_name")?.Value ?? "";
        if (username.Length > 100 || first.Length > 100 || last.Length > 100 ||
            input.ClientRequestId == Guid.Empty || input.Purpose is not ("new" or "change") ||
            input.RoleIds.Length > 20 || input.RoleIds.Distinct().Count() != input.RoleIds.Length ||
            (input.Purpose == "new" && input.RoleIds.Length == 0) || input.Note?.Length > 1000)
            return RoleRequestResult.Failure(400, "role_request.invalid");

        return await Locked(async () =>
        {
            // A confirmed subject uses its registered address. Only new identities rely on a verified token email.
            var confirmedEmail = await db.AppUsers.AsNoTracking()
                .Where(user => user.KeycloakSubjectConfirmed && user.KeycloakId == subject)
                .Select(user => user.Email).SingleOrDefaultAsync(ct);
            var tokenEmail = Principal.FindFirst(ClaimTypes.Email)?.Value ?? Principal.FindFirst("email")?.Value;
            var email = (confirmedEmail ?? (verified ? tokenEmail : null))?.Trim().ToLowerInvariant();
            if (email is not { Length: > 0 and <= 200 } ||
                !MailAddress.TryCreate(email, out var address) || address.Address != email)
                return RoleRequestResult.Failure(403, "identity.verified_email_required");

            if (await IsInactiveApplicant(subject, email, ct)) return RoleRequestResult.Failure(403, "identity.inactive");
            var prior = await Query().SingleOrDefaultAsync(row => row.RequesterSubject == subject && row.ClientRequestId == input.ClientRequestId, ct);
            if (prior is not null)
            {
                if (prior.Purpose != input.Purpose || prior.Note != input.Note?.Trim() ||
                    !prior.RequestedRoles.Select(item => item.RoleId).Order().SequenceEqual(input.RoleIds.Order()))
                    return Conflict("role_request.idempotency_mismatch");
                return new(Map(prior, false), 200);
            }
            if (await db.RoleRequests.AnyAsync(row => row.RequesterSubject == subject && row.Status == "pending", ct))
                return Conflict("role_request.pending_exists");
            var roles = await db.Roles.Where(role => input.RoleIds.Contains(role.Id) && role.IsActive && role.Name != ApplicationAccessRoles.Guest).ToListAsync(ct);
            if (roles.Count != input.RoleIds.Length) return InvalidRoles();
            var admins = await ActiveAdmins().ToListAsync(ct);
            if (admins.Count == 0) return RoleRequestResult.Failure(503, "role_request.approver_unavailable");
            var row = new RoleRequest { ClientRequestId = input.ClientRequestId, RequesterSubject = subject,
                Username = username, FirstName = first, LastName = last, Email = email, Purpose = input.Purpose, Note = input.Note?.Trim() };
            row.RequestedRoles = roles.Select(role => new RequestedRole { Role = role, RoleId = role.Id, RequestId = row.Id }).ToList();
            db.RoleRequests.Add(row);
            db.AuditLogs.Add(Audit("ROLE_REQUEST", null, new { row.Purpose, Roles = roles.Select(role => role.Name) }));
            foreach (var admin in admins)
                Notify(admin.KeycloakId, admin.Email, $"role-request:{row.Id}:submitted", "role_request.submitted", row, $"/app/admin/role-requests?requestId={row.Id}");
            await db.SaveChangesAsync(ct);
            return new(Map(row, false), 201);
        }, ct);
    }

    public async Task<PagedResponse<RoleRequestDTO>> List(bool review, string? status, int page, int pageSize, CancellationToken ct, long? requestId = null)
    {
        var query = Query().AsNoTracking();
        if (!review) { var subject = Subject; query = query.Where(row => row.RequesterSubject == subject); }
        if (requestId is { } id) query = query.Where(row => row.Id == id);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(row => row.Status == status);
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 200);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(row => row.CreatedAt).ThenBy(row => row.Id)
            .Skip((int)Math.Min((long)(page - 1) * pageSize, int.MaxValue)).Take(pageSize).ToListAsync(ct);
        return new(rows.Select(row => Map(row, review)).ToArray(), total, page, pageSize);
    }

    public async Task<RoleRequestResult> Decide(long id, bool approve, RoleRequestDecisionDTO input, CancellationToken ct)
    {
        if (input.DecisionId == Guid.Empty || input.ExpectedVersion < 1 || input.Note?.Length > 1000)
            return RoleRequestResult.Failure(400, "role_request.invalid_decision");
        return await Locked(async () =>
        {
            var subject = Subject;
            if (!await IsAdmin(subject, ct)) return RoleRequestResult.Failure(403, "authorization.admin_required");
            var row = await Query().SingleOrDefaultAsync(row => row.Id == id, ct);
            if (row is null) return RoleRequestResult.Failure(404, "request.not_found");
            if (row.RequesterSubject == subject) return RoleRequestResult.Failure(403, "role_request.self_decision_denied");
            var outcome = approve ? "approved" : "rejected";
            if (row.Status != "pending")
            {
                if (row.DecisionId == input.DecisionId && row.Status == outcome && row.ProcessedBy == Subject && row.DecisionNote == input.Note?.Trim()) return new(Map(row, true), 200);
                return Conflict("role_request.already_processed");
            }
            if (row.Version != input.ExpectedVersion) return Conflict("request.concurrent_change");
            string[]? oldRoles = null;
            string[]? resultingRoles = null;
            if (approve)
            {
                if (await IsInactiveApplicant(row.RequesterSubject, row.Email, ct)) return RoleRequestResult.Failure(403, "identity.inactive");
                if (row.RequestedRoles.Any(item => !item.Role.IsActive || item.Role.Name == ApplicationAccessRoles.Guest)) return InvalidRoles();
                var user = await db.AppUsers.SingleOrDefaultAsync(user => user.KeycloakSubjectConfirmed && user.KeycloakId == row.RequesterSubject, ct);
                var createUser = user is null;
                if (user is null)
                {
                    // Never take over a confirmed/legacy account by matching email or username.
                    var email = row.Email.ToUpperInvariant(); var name = row.Username.ToUpperInvariant();
                    if (await db.AppUsers.AnyAsync(user => user.KeycloakId == row.RequesterSubject || user.Email.Trim().ToUpper() == email || user.Username.Trim().ToUpper() == name, ct))
                        return Conflict("identity.reconciliation_required");
                    user = new AppUser { KeycloakId = row.RequesterSubject, KeycloakSubjectConfirmed = true,
                        Username = row.Username, FirstName = row.FirstName,
                        LastName = row.LastName, Email = row.Email, CreatedBy = AuthenticatedActor.Username(Principal) };

                }
                var existing = await db.UserRoles.Where(item => item.UserId == user.Id).Include(item => item.Role).ToListAsync(ct);
                oldRoles = existing.Where(item => item.IsActive).Select(item => item.Role.Name).ToArray();
                var targets = row.RequestedRoles.Select(item => item.RoleId).ToHashSet();
                if (row.Purpose == "new") targets.UnionWith(existing.Where(item => item.IsActive).Select(item => item.RoleId));
                resultingRoles = row.RequestedRoles.Select(item => item.Role.Name)
                    .Concat(existing.Where(item => targets.Contains(item.RoleId)).Select(item => item.Role.Name))
                    .Distinct(StringComparer.Ordinal).Order().ToArray();
                var removesAdmin = existing.Any(item => item.IsActive && item.Role.Name == ApplicationAccessRoles.Administrator && !targets.Contains(item.RoleId));
                if (removesAdmin && await ActiveAdmins().Select(user => user.Id).Distinct().Take(2).CountAsync(ct) <= 1)
                    return Conflict("users.final_admin");
                if (createUser) db.AppUsers.Add(user);
                var actor = AuthenticatedActor.Username(Principal);
                var modifiedAt = DateTime.UtcNow;
                foreach (var assignment in existing)
                    assignment.SetActive(targets.Contains(assignment.RoleId), actor, modifiedAt);
                foreach (var roleId in targets.Except(existing.Select(item => item.RoleId)))
                    db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId, CreatedBy = actor });
                user.ModifiedAt = modifiedAt;
                user.ModifiedBy = actor;
            }
            var before = new { row.Status, row.DecisionNote, EffectiveRoles = oldRoles };
            row.Status = outcome; row.DecisionId = input.DecisionId; row.DecisionNote = input.Note?.Trim();
            row.ProcessedAt = DateTime.UtcNow; row.ProcessedBy = Subject; row.Version++;
            db.AuditLogs.Add(Audit(approve ? "ROLE_REQUEST_APPROVED" : "ROLE_REQUEST_REJECTED", before,
                new { row.Status, row.DecisionNote, EffectiveRoles = resultingRoles }));
            var recipientEmail = await db.AppUsers.Where(user => user.KeycloakSubjectConfirmed && user.KeycloakId == row.RequesterSubject)
                .Select(user => user.Email).SingleOrDefaultAsync(ct) ?? row.Email;
            Notify(row.RequesterSubject, recipientEmail, $"role-request:{row.Id}:{outcome}", "role_request." + outcome, row, $"/app/profile?requestId={row.Id}");
            await db.SaveChangesAsync(ct); // Request, grants, audit, inbox and outbox share one commit.
            return new(Map(row, true), 200);
        }, ct);
    }

    private IQueryable<RoleRequest> Query() => db.RoleRequests.Include(row => row.RequestedRoles).ThenInclude(item => item.Role);
    private IQueryable<AppUser> ActiveAdmins() => db.AppUsers.Where(user => user.IsActive && user.KeycloakSubjectConfirmed &&
        db.UserRoles.Any(assignment => assignment.UserId == user.Id && assignment.IsActive && assignment.Role.IsActive && assignment.Role.Name == ApplicationAccessRoles.Administrator &&
            assignment.Role.RolePermissions.Any(grant => grant.Permission.IsActive && grant.Permission.Code == ApplicationPermissions.AdministrationManage)));
    public Task<bool> CanReview(CancellationToken ct) => IsAdmin(Subject, ct);
    private Task<bool> IsAdmin(string subject, CancellationToken ct) =>
        ActiveAdmins().AnyAsync(user => user.KeycloakId == subject, ct);

    private Task<bool> IsInactiveApplicant(string subject, string email, CancellationToken ct)
    {
        var normalized = email.ToUpperInvariant();
        return db.AppUsers.AnyAsync(user => !user.IsActive && (user.KeycloakId == subject || user.Email.Trim().ToUpper() == normalized), ct);
    }

    private Task<RoleRequestResult> Locked(Func<Task<RoleRequestResult>> mutation, CancellationToken ct) =>
        UsersMutationBoundary.ExecuteAsync(db, mutation, () => Conflict("request.busy"), ct);
    private static RoleRequestResult Conflict(string code) => RoleRequestResult.Failure(409, code);
    private static RoleRequestResult InvalidRoles() => RoleRequestResult.Failure(400, "role_request.invalid_roles");
    private static RoleRequestDTO Map(RoleRequest row, bool review) => new(row.Id, row.Purpose, row.Status,
        row.RequestedRoles.Select(item => item.RoleId).Order().ToArray(), row.RequestedRoles.Select(item => item.Role.Name).Order().ToArray(),
        row.Note, row.CreatedAt, row.ProcessedAt, row.ProcessedBy, row.DecisionNote, row.Version,
        review ? row.Username : null, review ? row.Email : null);
    private AuditLog Audit(string action, object? before, object after) => new()
    {
        TableName = "RoleRequest", Action = action,
        Username = Principal.FindFirst("preferred_username")?.Value ?? Subject,

        OldValues = before is null ? null : JsonSerializer.Serialize(before), NewValues = JsonSerializer.Serialize(after)
    };
    private void Notify(string recipient, string email, string eventKey, string messageKey, RoleRequest row, string route)
    {
        var notice = new ApplicationNotification { RecipientSubject = recipient, RecipientEmail = email, EventKey = eventKey,
            TitleKey = "notifications." + messageKey.Replace('_', '.') + ".title", BodyKey = "notifications." + messageKey.Replace('_', '.') + ".detail",
            ParametersJson = JsonSerializer.Serialize(new { requester = row.Username, requestId = row.Id, status = row.Status,
                roles = string.Join(", ", row.RequestedRoles.Select(item => item.Role.Name)), note = row.Note ?? "", decisionNote = row.DecisionNote ?? "" }), Route = route };
        notice.Delivery = new NotificationDelivery { Notification = notice };
        db.Notifications.Add(notice);
    }
}
