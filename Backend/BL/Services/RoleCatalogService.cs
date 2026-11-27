using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.DTO.Roles;
using RBBH.ConnectedParties.DL.Entities.Role;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions;
using RBBH.ConnectedParties.Helpers;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>Maintains DB-backed role definitions without replacing normalized grants or provider identity.</summary>
public sealed class RoleCatalogService(ConnectedPartiesDbContext db, IHttpContextAccessor accessor)
{
    private System.Security.Claims.ClaimsPrincipal Actor => accessor.HttpContext?.User ?? throw new MissingActorIdentityException();
    private static DateTime VersionOf(Role role) =>
        DateTime.SpecifyKind(role.ModifiedAt ?? role.CreatedAt, DateTimeKind.Utc);

    private static bool Protected(string name) => name.Equals(ApplicationAccessRoles.Administrator, StringComparison.OrdinalIgnoreCase) ||
        name.Equals(ApplicationAccessRoles.Guest, StringComparison.OrdinalIgnoreCase);

    public async Task<object> List(int page, int pageSize, string? search, CancellationToken ct)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Roles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(row => row.Name.ToUpper().Contains(term));
        }
        var rows = await query.Include(row => row.RolePermissions).OrderBy(row => row.Name).ThenBy(row => row.Id)
            .Skip((int)Math.Min((long)(page - 1) * pageSize, int.MaxValue)).Take(pageSize).ToListAsync(ct);
        var total = page == 1 && rows.Count < pageSize ? rows.Count : await query.CountAsync(ct);
        return new { items = rows.Select(Map).ToArray(), total, page, pageSize };
    }

    public async Task<RoleCatalogDTO> Get(long id, CancellationToken ct)
    {
        var row = await db.Roles.AsNoTracking().Include(row => row.RolePermissions).SingleOrDefaultAsync(row => row.Id == id, ct)
            ?? throw new BusinessRuleException(404, "roles.not_found");
        return Map(row);
    }

    public async Task<object> Permissions(CancellationToken ct) => new
    {
        items = await db.Permissions.AsNoTracking().Where(row => row.IsActive).OrderBy(row => row.Module).ThenBy(row => row.Code)
            .Select(row => new { row.Id, row.Code, row.Module, Description = row.Code }).ToListAsync(ct)
    };

    public Task<RoleCatalogDTO?> Create(CreateRoleCatalogDTO input, CancellationToken ct) => Locked<RoleCatalogDTO?>(async () =>
    {
        await EnsureAdmin(ct);
        var name = input.Name?.Trim() ?? "";
        if (name.Length is < 1 or > 100 || !name.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.') || Protected(name))
            throw Invalid();
        Validate(input.PermissionIds);
        var normalized = name.ToUpperInvariant();
        if (await db.Roles.AnyAsync(row => row.Name.Trim().ToUpper() == normalized, ct))
            return null;
        await ValidatePermissions(input.PermissionIds, ct);
        var row = new Role { Name = name,
            CreatedBy = AuthenticatedActor.Username(Actor) };
        foreach (var id in input.PermissionIds) row.RolePermissions.Add(new RolePermission { RoleId = row.Id, PermissionId = id });
        db.Roles.Add(row);
        db.AuditLogs.Add(BusinessAudit.Create("Role", "ROLE_CATALOG_CREATE", AuthenticatedActor.Username(Actor), accessor, null,
            new { row.Name, row.IsActive, PermissionIds = input.PermissionIds }));
        await db.SaveChangesAsync(ct); return Map(row);
    }, ct);

    public Task<RoleCatalogDTO> Update(long id, UpdateRoleCatalogDTO input, CancellationToken ct) => Locked<RoleCatalogDTO>(async () =>
    {
        await EnsureAdmin(ct);
        Validate(input.PermissionIds);
        var row = await db.Roles.Include(row => row.RolePermissions).SingleOrDefaultAsync(row => row.Id == id, ct)
            ?? throw new BusinessRuleException(404, "roles.not_found");
        if (Protected(row.Name)) throw new BusinessRuleException(409, "roles.protected");
        var subject = AuthenticatedActor.RequireSubject(Actor);
        if (await db.UserRoles.AnyAsync(assignment => assignment.RoleId == id && assignment.IsActive &&
            db.AppUsers.Any(user => user.Id == assignment.UserId && user.KeycloakSubjectConfirmed && user.KeycloakId == subject), ct))
            throw new BusinessRuleException(403, "roles.self_management_denied");
        if (input.ExpectedVersion != VersionOf(row)) throw new BusinessRuleException(409, "request.concurrent_change");
        await ValidatePermissions(input.PermissionIds, ct);
        var before = new { row.Name, row.IsActive, PermissionIds = row.RolePermissions.Select(grant => grant.PermissionId).ToArray() };
        var targets = input.PermissionIds.ToHashSet();
        db.RolePermissions.RemoveRange(row.RolePermissions.Where(grant => !targets.Contains(grant.PermissionId)));
        foreach (var permissionId in targets.Except(row.RolePermissions.Select(grant => grant.PermissionId)))
            row.RolePermissions.Add(new RolePermission { RoleId = row.Id, PermissionId = permissionId });
        row.IsActive = input.IsActive;
        var previous = VersionOf(row);
        var now = DateTime.UtcNow;
        row.ModifiedAt = now > previous ? now : previous.AddTicks(1);
        row.ModifiedBy = AuthenticatedActor.Username(Actor);
        db.AuditLogs.Add(BusinessAudit.Create("Role", "ROLE_CATALOG_UPDATE", AuthenticatedActor.Username(Actor), accessor, before,
            new { row.Name, row.IsActive, PermissionIds = input.PermissionIds }));
        await db.SaveChangesAsync(ct);
        // Removed tracked children need not remain in the returned grant set.
        return new(row.Id, row.Name, row.Name, null, row.IsActive, VersionOf(row), input.PermissionIds.Order().ToArray(), false);
    }, ct);

    public Task<RoleDeletionResult> Delete(long id, DateTime expectedVersion, CancellationToken ct) => Locked(async () =>
    {
        await EnsureAdmin(ct);
        var row = await db.Roles.Include(role => role.RolePermissions).SingleOrDefaultAsync(role => role.Id == id, ct);
        if (row is null) return RoleDeletionResult.Missing;
        if (Protected(row.Name)) return RoleDeletionResult.Protected;
        if (expectedVersion != VersionOf(row)) return RoleDeletionResult.Stale;
        if (await db.UserRoles.AnyAsync(assignment => assignment.RoleId == id, ct) ||
            await db.Set<DL.Entities.Authorization.RequestedRole>().AnyAsync(request => request.RoleId == id, ct))
            return RoleDeletionResult.InUse;

        db.AuditLogs.Add(BusinessAudit.Create("Role", "ROLE_CATALOG_DELETE",
            AuthenticatedActor.Username(Actor), accessor,
            new { row.Name, row.IsActive, PermissionIds = row.RolePermissions.Select(grant => grant.PermissionId).ToArray() }, null));
        db.Roles.Remove(row);
        await db.SaveChangesAsync(ct);
        return RoleDeletionResult.Deleted;
    }, ct);

    public enum RoleDeletionResult { Deleted, Missing, Protected, Stale, InUse }

    private async Task EnsureAdmin(CancellationToken ct)
    {
        var subject = AuthenticatedActor.RequireSubject(Actor);
        if (!await db.AppUsers.AnyAsync(user => user.IsActive && user.KeycloakSubjectConfirmed && user.KeycloakId == subject &&
            db.UserRoles.Any(assignment => assignment.UserId == user.Id && assignment.IsActive && assignment.Role.IsActive &&
                assignment.Role.Name == ApplicationAccessRoles.Administrator && assignment.Role.RolePermissions.Any(grant =>
                    grant.Permission.IsActive && grant.Permission.Code == ApplicationPermissions.AdministrationManage)), ct))
            throw new BusinessRuleException(403, "authorization.admin_required");
    }
    private static void Validate(long[]? ids)
    {
        if (ids is null || ids.Length > 200 ||
            ids.Any(id => id == 0) || ids.Distinct().Count() != ids.Length) throw Invalid();
    }
    private async Task ValidatePermissions(long[] ids, CancellationToken ct)
    {
        if (await db.Permissions.CountAsync(row => ids.Contains(row.Id) && row.IsActive, ct) != ids.Length)
            throw new BusinessRuleException(400, "roles.invalid_permissions");
    }
    private Task<T> Locked<T>(Func<Task<T>> mutation, CancellationToken ct) => UsersMutationBoundary.ExecuteAsync(db, mutation,
        () => throw new BusinessRuleException(409, "request.busy"), ct);
    private static BusinessRuleException Invalid() => new(400, "roles.invalid_definition");
    private static RoleCatalogDTO Map(Role row) => new(row.Id, row.Name, row.Name, null,
        row.IsActive, VersionOf(row), row.RolePermissions.Select(grant => grant.PermissionId).Order().ToArray(), Protected(row.Name));
}
