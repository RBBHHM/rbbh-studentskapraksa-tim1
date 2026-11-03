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
    private static bool Protected(string name) => name.Equals(ApplicationAccessRoles.Administrator, StringComparison.OrdinalIgnoreCase) ||
        name.Equals(ApplicationAccessRoles.Guest, StringComparison.OrdinalIgnoreCase);

    public async Task<object> List(int page, int pageSize, string? search, CancellationToken ct)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Roles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(row => row.Name.ToUpper().Contains(term) || row.DisplayName != null && row.DisplayName.ToUpper().Contains(term));
        }
        var total = await query.CountAsync(ct);
        var rows = await query.Include(row => row.RolePermissions).OrderBy(row => row.Name).ThenBy(row => row.Id)
            .Skip((int)Math.Min((long)(page - 1) * pageSize, int.MaxValue)).Take(pageSize).ToListAsync(ct);
        return new { items = rows.Select(Map).ToArray(), total, page, pageSize };
    }

    public async Task<RoleCatalogDTO> Get(Guid id, CancellationToken ct)
    {
        var row = await db.Roles.AsNoTracking().Include(row => row.RolePermissions).SingleOrDefaultAsync(row => row.Id == id, ct)
            ?? throw new BusinessRuleException(404, "roles.not_found", "Rola nije pronađena.");
        return Map(row);
    }

    public async Task<object> Permissions(CancellationToken ct) => new
    {
        items = await db.Permissions.AsNoTracking().Where(row => row.IsActive).OrderBy(row => row.Module).ThenBy(row => row.Code)
            .Select(row => new { row.Id, row.Code, row.Module, row.Description }).ToListAsync(ct)
    };

    public Task<RoleCatalogDTO> Create(CreateRoleCatalogDTO input, CancellationToken ct) => Locked<RoleCatalogDTO>(async () =>
    {
        await EnsureAdmin(ct);
        var name = input.Name?.Trim() ?? "";
        if (name.Length is < 1 or > 100 || !name.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.') || Protected(name))
            throw Invalid();
        Validate(input.DisplayName, input.Description, input.PermissionIds);
        var normalized = name.ToUpperInvariant();
        if (await db.Roles.AnyAsync(row => row.Name.Trim().ToUpper() == normalized, ct))
            throw new BusinessRuleException(409, "roles.duplicate_name", "Rola s odabranim nazivom već postoji.");
        await ValidatePermissions(input.PermissionIds, ct);
        var row = new Role { Name = name, DisplayName = input.DisplayName.Trim(), Description = input.Description?.Trim(),
            CreatedBy = AuthenticatedActor.Username(Actor) };
        foreach (var id in input.PermissionIds) row.RolePermissions.Add(new RolePermission { RoleId = row.Id, PermissionId = id });
        db.Roles.Add(row);
        db.AuditLogs.Add(BusinessAudit.Create("Role", row.Id.ToString(), "ROLE_CATALOG_CREATE", AuthenticatedActor.Username(Actor), accessor, null,
            new { row.Name, row.DisplayName, row.Description, row.IsActive, PermissionIds = input.PermissionIds }));
        await db.SaveChangesAsync(ct); return Map(row);
    }, ct);

    public Task<RoleCatalogDTO> Update(Guid id, UpdateRoleCatalogDTO input, CancellationToken ct) => Locked<RoleCatalogDTO>(async () =>
    {
        await EnsureAdmin(ct);
        Validate(input.DisplayName, input.Description, input.PermissionIds);
        var row = await db.Roles.Include(row => row.RolePermissions).SingleOrDefaultAsync(row => row.Id == id, ct)
            ?? throw new BusinessRuleException(404, "roles.not_found", "Rola nije pronađena.");
        if (Protected(row.Name)) throw new BusinessRuleException(409, "roles.protected", "Tehničku Admin ili Guest definiciju nije moguće mijenjati kroz katalog.");
        var subject = AuthenticatedActor.RequireSubject(Actor);
        if (await db.UserRoles.AnyAsync(assignment => assignment.RoleId == id && assignment.IsActive &&
            db.AppUsers.Any(user => user.Id == assignment.UserId && user.KeycloakSubjectConfirmed && user.KeycloakId == subject), ct))
            throw new BusinessRuleException(403, "roles.self_management_denied", "Nije dozvoljeno mijenjati definiciju vlastite dodijeljene role.");
        if (input.ExpectedVersion != row.Version) throw new BusinessRuleException(409, "request.concurrent_change", "Rola je promijenjena. Osvježite podatke.");
        await ValidatePermissions(input.PermissionIds, ct);
        var before = new { row.Name, row.DisplayName, row.Description, row.IsActive, PermissionIds = row.RolePermissions.Select(grant => grant.PermissionId).ToArray() };
        var targets = input.PermissionIds.ToHashSet();
        db.RolePermissions.RemoveRange(row.RolePermissions.Where(grant => !targets.Contains(grant.PermissionId)));
        foreach (var permissionId in targets.Except(row.RolePermissions.Select(grant => grant.PermissionId)))
            row.RolePermissions.Add(new RolePermission { RoleId = row.Id, PermissionId = permissionId });
        row.DisplayName = input.DisplayName.Trim(); row.Description = input.Description?.Trim(); row.IsActive = input.IsActive;
        row.ModifiedAt = DateTime.UtcNow; row.ModifiedBy = AuthenticatedActor.Username(Actor); row.Version++;
        db.AuditLogs.Add(BusinessAudit.Create("Role", row.Id.ToString(), "ROLE_CATALOG_UPDATE", AuthenticatedActor.Username(Actor), accessor, before,
            new { row.Name, row.DisplayName, row.Description, row.IsActive, PermissionIds = input.PermissionIds }));
        await db.SaveChangesAsync(ct);
        // Removed tracked children need not remain in the returned grant set.
        return new(row.Id, row.Name, row.DisplayName, row.Description, row.IsActive, row.Version, input.PermissionIds.Order().ToArray(), false);
    }, ct);

    private async Task EnsureAdmin(CancellationToken ct)
    {
        var subject = AuthenticatedActor.RequireSubject(Actor);
        if (!await db.AppUsers.AnyAsync(user => user.IsActive && user.KeycloakSubjectConfirmed && user.KeycloakId == subject &&
            db.UserRoles.Any(assignment => assignment.UserId == user.Id && assignment.IsActive && assignment.Role.IsActive &&
                assignment.Role.Name == ApplicationAccessRoles.Administrator && assignment.Role.RolePermissions.Any(grant =>
                    grant.Permission.IsActive && grant.Permission.Code == ApplicationPermissions.AdministrationManage)), ct))
            throw new BusinessRuleException(403, "authorization.admin_required", "Za ovu radnju su potrebne administratorske dozvole.");
    }
    private static void Validate(string? display, string? description, Guid[]? ids)
    {
        if (string.IsNullOrWhiteSpace(display) || display.Length > 100 || description?.Length > 500 || ids is null || ids.Length > 200 ||
            ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length) throw Invalid();
    }
    private async Task ValidatePermissions(Guid[] ids, CancellationToken ct)
    {
        if (await db.Permissions.CountAsync(row => ids.Contains(row.Id) && row.IsActive, ct) != ids.Length)
            throw new BusinessRuleException(400, "roles.invalid_permissions", "Odabrane dozvole nisu dostupne.");
    }
    private Task<T> Locked<T>(Func<Task<T>> mutation, CancellationToken ct) => UsersMutationBoundary.ExecuteAsync(db, mutation,
        () => throw new BusinessRuleException(409, "request.busy", "Izmjena je u toku. Pokušajte ponovo."), ct);
    private static BusinessRuleException Invalid() => new(400, "roles.invalid_definition", "Provjerite naziv, opis i dozvole role.");
    private static RoleCatalogDTO Map(Role row) => new(row.Id, row.Name, row.DisplayName ?? row.Name, row.Description,
        row.IsActive, row.Version, row.RolePermissions.Select(grant => grant.PermissionId).Order().ToArray(), Protected(row.Name));
}
