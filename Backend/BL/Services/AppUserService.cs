using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.Exceptions.Validations;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.BL.Services;

public partial class AppUserService(ConnectedPartiesDbContext dbContext, KeycloakAdminService keycloakAdmin, IAuditService audit, IHttpContextAccessor? accessor = null) : IAppUserService
{
    private readonly ConnectedPartiesDbContext _db = dbContext;
    private readonly KeycloakAdminService _keycloakAdmin = keycloakAdmin;
    private readonly IAuditService _audit = audit;

    public async Task<Result<GetUsersResponseDTO>> GetUsersAsync(
        int page, int pageSize, string? search, string? role)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = _db.AppUsers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = _db.Database.IsRelational()
                ? query.Where(u => EF.Functions.Like(u.FirstName, $"%{term}%")
                    || EF.Functions.Like(u.LastName, $"%{term}%")
                    || EF.Functions.Like(u.Username, $"%{term}%"))
                : query.Where(u => u.FirstName.ToLower().Contains(term.ToLower())
                    || u.LastName.ToLower().Contains(term.ToLower())
                    || u.Username.ToLower().Contains(term.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var normalizedRole = role.ToUpperInvariant();
            query = query.Where(user => _db.UserRoles.Any(assignment => assignment.UserId == user.Id && assignment.IsActive &&
                assignment.Role.IsActive && assignment.Role.Name.ToUpper() == normalizedRole));
        }
        var total = await query.CountAsync();
        var users = await query.OrderBy(user => user.LastName).ThenBy(user => user.FirstName).ThenBy(user => user.Id)
            .Skip((int)Math.Min((long)(page - 1) * pageSize, int.MaxValue)).Take(pageSize).ToListAsync();
        var userIds = users.Select(user => user.Id).ToList();
        var userRoles = await _db.UserRoles.AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId) && ur.IsActive)
            .Join(_db.Roles.Where(role => role.IsActive && role.Name != ApplicationAccessRoles.Guest),
                ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync();
        var rolesByUser = userRoles.GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());
        var grants = await _db.RolePermissions.Where(mapping => mapping.Role.IsActive && mapping.Permission.IsActive)
            .Select(mapping => new { Role = mapping.Role.Name, Permission = mapping.Permission.Code }).ToListAsync();

        var result = users.Select(u => new UserDTO
        {
            Id = u.Id,
            Username = u.Username,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            IsActive = u.IsActive,
            IdentityConfirmed = u.KeycloakSubjectConfirmed,
            Roles = rolesByUser.TryGetValue(u.Id, out var r) ? r : [],
            EffectivePermissions = u.IsActive ? grants.Where(grant => u.KeycloakSubjectConfirmed &&
                    rolesByUser.TryGetValue(u.Id, out var assignedRoles) && assignedRoles.Contains(grant.Role))
                .Select(grant => grant.Permission).Concat(RolePermissionMatrix.PermissionsByRole[ApplicationAccessRoles.Guest])
                .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList() : []
        }).ToList();
        return Result<GetUsersResponseDTO>.Success(new GetUsersResponseDTO
        {
            Users = result,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<UserDTO>> CreateUserAsync(CreateUserDTO dto, string createdBy, string? ipAddress = null)
    {
        var requestedRoleIds = dto.RoleIds
            .Concat(Guid.TryParse(dto.RoleId, out var legacyRoleId) ? [legacyRoleId] : [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        var ct = accessor?.HttpContext?.RequestAborted ?? CancellationToken.None;
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        if (!System.Net.Mail.MailAddress.TryCreate(normalizedEmail, out var email) || email.Address != normalizedEmail || normalizedEmail.Length > 200)
            return Result<UserDTO>.ValidationError("Email adresa nije ispravna.");
        var keycloakId = Guid.NewGuid().ToString();
        if (_keycloakAdmin.IsEnabled)
        {
            var identity = await _keycloakAdmin.FindByEmailAsync(normalizedEmail, ct);
            if (identity is null) return Result<UserDTO>.ValidationError("Korisnik nije pronađen u sistemu za prijavu.");
            keycloakId = identity.Subject;
            dto.Username = identity.Username ?? dto.Username;
            dto.FirstName = identity.FirstName ?? dto.FirstName;
            dto.LastName = identity.LastName ?? dto.LastName;
        }
        return await UsersMutationBoundary.ExecuteAsync(_db, async () =>
        {
        var username = dto.Username.Trim(); var normalizedName = username.ToUpperInvariant(); var normalizedAddress = normalizedEmail.ToUpperInvariant();
        if (string.IsNullOrEmpty(username) || username.Length > 100 || string.IsNullOrWhiteSpace(dto.FirstName) || dto.FirstName.Length > 100 || string.IsNullOrWhiteSpace(dto.LastName) || dto.LastName.Length > 100)
            return Result<UserDTO>.ValidationError("Provjerite ime, prezime i korisničko ime.");
        if (await _db.AppUsers.AnyAsync(user => user.Username.Trim().ToUpper() == normalizedName || user.Email.Trim().ToUpper() == normalizedAddress || user.KeycloakId == keycloakId, ct))
            return Result<UserDTO>.ValidationError("Korisnik sa navedenim identitetom već postoji.");
        var localRoles = await _db.Roles.Where(role => requestedRoleIds.Contains(role.Id) && role.IsActive).ToListAsync(ct);
        if (localRoles.Count != requestedRoleIds.Length || localRoles.Any(role => role.Name == ApplicationAccessRoles.Guest))
            return Result<UserDTO>.ValidationError("Odabrane aplikacijske role nisu ispravne.");
        var roleNames = localRoles.Select(role => role.Name).ToList();
        var user = new AppUser
        {
            KeycloakId = keycloakId, // Unconfirmed placeholder when provider administration is disabled.
            KeycloakSubjectConfirmed = _keycloakAdmin.IsEnabled && !string.IsNullOrEmpty(keycloakId),
            KeycloakSubjectLinkedAt = _keycloakAdmin.IsEnabled ? DateTime.UtcNow : null,
            Username = dto.Username.Trim(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = normalizedEmail,
            IsActive = dto.IsActive,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.AppUsers.Add(user);

        foreach (var role in localRoles)
        {
            _db.UserRoles.Add(new DL.Entities.Role.UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = role.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            });
        }

        await _db.SaveChangesAsync(ct);

        // Audit log — CREATE
        await _audit.LogAsync(new AuditEntry
        {
            TableName = "AppUser",
            RecordId = keycloakId,
            Action = "CREATE",
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            { user.Username, user.Email, user.IsActive, Roles = roleNames }),
            UserId = createdBy,
            Username = createdBy,
            IpAddress = ipAddress
        });

        return Result<UserDTO>.Success(new UserDTO
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            IsActive = user.IsActive,
            IdentityConfirmed = user.KeycloakSubjectConfirmed,
            Roles = roleNames,
            EffectivePermissions = user.IsActive ? (await _db.RolePermissions
                .Where(mapping => user.KeycloakSubjectConfirmed && mapping.Role.IsActive && mapping.Permission.IsActive && roleNames.Contains(mapping.Role.Name))
                .Select(mapping => mapping.Permission.Code).ToListAsync())
                .Concat(RolePermissionMatrix.PermissionsByRole[ApplicationAccessRoles.Guest]).Distinct(StringComparer.Ordinal).ToList() : []
        });
        }, () => Result<UserDTO>.ValidationError("Izmjena je u toku. Pokušajte ponovo."), ct);
    }
}
