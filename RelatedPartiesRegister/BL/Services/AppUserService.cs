using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Users;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.Exceptions.Validations;
using Mapster;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.BL.Services;

public class AppUserService(ConnectedPartiesDbContext dbContext, KeycloakAdminService keycloakAdmin, IAuditService audit) : IAppUserService
{
    private readonly ConnectedPartiesDbContext _db           = dbContext;
    private readonly KeycloakAdminService  _keycloakAdmin = keycloakAdmin;
    private readonly IAuditService         _audit         = audit;

    public async Task<Result<GetUsersResponseDTO>> GetUsersAsync(
        int page, int pageSize, string? search, string? role)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var users = await GetUsersFromDbAsync(search, role);

        var total = users.Count;
        var paged = users
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Result<GetUsersResponseDTO>.Success(new GetUsersResponseDTO
        {
            Users    = paged,
            Total    = total,
            Page     = page,
            PageSize = pageSize
        });
    }

    private async Task<List<UserDTO>> GetUsersFromDbAsync(string? search, string? role)
    {
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

        var users    = await query.ToListAsync();
        var userIds  = users.Select(u => u.Id).ToList();
        var userRoles = await _db.UserRoles.AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId) && ur.IsActive)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync();
        var rolesByUser = userRoles.GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var result = users.Select(u => new UserDTO
        {
            Id        = u.Id,
            Username  = u.Username,
            FirstName = u.FirstName,
            LastName  = u.LastName,
            Email     = u.Email,
            IsActive  = u.IsActive,
            Roles     = rolesByUser.TryGetValue(u.Id, out var r) ? r : [],
            EffectivePermissions = RolePermissionMatrix.Resolve(
                rolesByUser.TryGetValue(u.Id, out var assignedRoles) ? assignedRoles : []).ToList()
        }).ToList();
        if (!string.IsNullOrWhiteSpace(role))
            result = result.Where(user => user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase)).ToList();
        return result;
    }

    public async Task<Result<UserDTO>> CreateUserAsync(CreateUserDTO dto, string createdBy)
    {
        var requestedRoleIds = dto.RoleIds
            .Concat(Guid.TryParse(dto.RoleId, out var legacyRoleId) ? [legacyRoleId] : [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        if (requestedRoleIds.Length == 0)
            return Result<UserDTO>.ValidationError("Odaberite najmanje jedan funkcionalni pristup.");
        if (await _db.AppUsers.AnyAsync(u => u.Username == dto.Username))
            return Result<UserDTO>.ValidationError($"Korisnik s korisničkim imenom '{dto.Username}' već postoji.");
        var normalizedEmail = dto.Email.Trim().ToLower();
        if (!normalizedEmail.EndsWith("@raiffeisengroup.ba", StringComparison.OrdinalIgnoreCase))
            return Result<UserDTO>.ValidationError("Email adresa mora završavati domenom @raiffeisengroup.ba.");
        if (await _db.AppUsers.AnyAsync(u => u.Email == normalizedEmail))
            return Result<UserDTO>.ValidationError($"Korisnik s email adresom '{dto.Email}' već postoji.");

        var localRoles = await _db.Roles
            .Where(role => requestedRoleIds.Contains(role.Id) && role.IsActive)
            .ToListAsync();
        if (localRoles.Count != requestedRoleIds.Length || localRoles.Any(role => !ApplicationAccessRoles.Assignable.Contains(role.Name)))
            return Result<UserDTO>.ValidationError("Odabrane aplikacijske role nisu ispravne.");

        var roleNames = localRoles.Select(role => role.Name).ToList();
        var keycloakId = Guid.NewGuid().ToString();
        if (_keycloakAdmin.IsEnabled)
        {
            var keycloakUsers = await _keycloakAdmin.GetUsersAsync(normalizedEmail);
            if (!keycloakUsers.IsSuccessful)
                return Result<UserDTO>.InternalServerError(
                    keycloakUsers.ExceptionMessage ?? "Korisnika trenutno nije moguće pronaći u sistemu za prijavu.");
            var keycloakUser = keycloakUsers.Value.SingleOrDefault(user =>
                user.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));
            if (keycloakUser is null)
                return Result<UserDTO>.ValidationError("Korisnik s navedenom email adresom nije pronađen u Keycloaku.");
            keycloakId = keycloakUser.Id.ToString();
            dto.Username = keycloakUser.Username;
            dto.FirstName = keycloakUser.FirstName;
            dto.LastName = keycloakUser.LastName;
        }

        var user = new AppUser
        {
            KeycloakId = string.IsNullOrEmpty(keycloakId) ? Guid.NewGuid().ToString() : keycloakId,
            Username   = dto.Username.Trim(),
            FirstName  = dto.FirstName.Trim(),
            LastName   = dto.LastName.Trim(),
            Email      = normalizedEmail,
            IsActive   = dto.IsActive,
            CreatedBy  = createdBy,
            CreatedAt  = DateTime.UtcNow
        };

        _db.AppUsers.Add(user);

        foreach (var role in localRoles)
        {
            _db.UserRoles.Add(new DL.Entities.Role.UserRole
            {
                Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id,
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = createdBy
            });
        }

        await _db.SaveChangesAsync();

        // Audit log — CREATE
        await _audit.LogAsync(new AuditEntry
        {
            TableName = "AppUser",
            RecordId  = keycloakId,
            Action    = "CREATE",
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
                { user.Username, user.Email, user.IsActive, Roles = roleNames }),
            UserId    = createdBy,
            Username  = createdBy
        });

        return Result<UserDTO>.Success(new UserDTO
        {
            Id        = user.Id,
            Username  = user.Username,
            FirstName = user.FirstName,
            LastName  = user.LastName,
            Email     = user.Email,
            IsActive  = user.IsActive,
            Roles     = roleNames,
            EffectivePermissions = RolePermissionMatrix.Resolve(roleNames).ToList()
        });
    }
}
