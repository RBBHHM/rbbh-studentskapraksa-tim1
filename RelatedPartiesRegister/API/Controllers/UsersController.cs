using RBBH.ConnectedParties.API.Controllers.BaseController;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.Roles;
using RBBH.ConnectedParties.DL.DTO.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.DL.Entities.Users;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

/// <summary>
/// US2: Upravljanje korisnicima — lista, kreiranje, dodjela rola.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = "application-administration")]
public class UsersController(IAppUserService appUserService, KeycloakAdminService keycloakAdmin, IAuditService audit, ConnectedPartiesDbContext db)
    : BaseResuItController
{
    private readonly IAppUserService     _appUserService  = appUserService;
    private readonly KeycloakAdminService _keycloakAdmin  = keycloakAdmin;
    private readonly IAuditService       _audit           = audit;
    private readonly ConnectedPartiesDbContext _db = db;

    private string CurrentUsername() =>
        User.FindFirst("preferred_username")?.Value
        ?? User.Identity?.Name
        ?? "system";

    /// <summary>
    /// Returns a paged list of all users.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetUsersResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetUsersResponseDTO>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null)
    {
        var result = await _appUserService.GetUsersAsync(page, pageSize, search, role);
        return HandleResult(result);
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDTO>> CreateUser([FromBody] CreateUserDTO dto)
    {
        var createdBy = User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                     ?? User.Identity?.Name
                     ?? "system";

        var result = await _appUserService.CreateUserAsync(dto, createdBy);

        if (result.IsSuccessful)
            return Created($"{Request.Path}/{result.Value.Id}", result.Value);

        return HTTPExceptiontFromResult(result);
    }

    /// <summary>Atomically replaces the user's functional application accesses.</summary>
    [HttpPost("{userId}/roles")]
    [HttpPut("{userId}/role")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> AssignOrUpdateUserRole(
        [FromRoute] string userId,
        [FromBody] AssignRoleRequestDTO request)
    {
        var roleIds = request.EffectiveRoleIds;
        if (roleIds.Count == 0)
            return BadRequest(new ProblemDetails { Title = "Odaberite pristup", Detail = "Korisnik mora imati najmanje jedan funkcionalni pristup." });

        if (!_keycloakAdmin.IsEnabled)
        {
            var localRoles = await _db.Roles
                .Where(role => roleIds.Contains(role.Id) && role.IsActive)
                .ToListAsync();
            if (localRoles.Count != roleIds.Count || localRoles.Any(role => !ApplicationAccessRoles.All.Contains(role.Name)))
                return BadRequest(new ProblemDetails { Title = "Neispravan pristup", Detail = "Dozvoljeni su samo pristupi fizičkim licima, pravnim licima, limitima i regulatornom izvještavanju." });
            if (!Guid.TryParse(userId, out var localUserId))
                return BadRequest(new ProblemDetails { Title = "Neispravni podaci", Detail = "Korisnik nije ispravan." });
            var localUser = await _db.AppUsers.FindAsync(localUserId);
            if (localUser is null) return NotFound(new ProblemDetails { Title = "Korisnik nije pronađen" });
            await ReplaceLocalRoles(localUser.Id, roleIds);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Pristupi korisnika su uspješno ažurirani.", roles = localRoles.Select(role => role.Name) });
        }

        var allRoles = await _keycloakAdmin.GetRealmRolesAsync();
        var targetRoles = allRoles.Value?
            .Where(role => roleIds.Contains(role.Id) && ApplicationAccessRoles.All.Contains(role.Name))
            .ToList() ?? [];
        if (targetRoles.Count != roleIds.Count)
            return BadRequest(new ProblemDetails { Title = "Pristupi nisu podešeni", Detail = "Jedan ili više odabrana pristupa ne postoje u Keycloaku." });

        var existingRoles = await _keycloakAdmin.GetUserRealmRolesAsync(userId);
        var existingApplicationRoles = existingRoles.Where(role => ApplicationAccessRoles.All.Contains(role.Name)).ToList();
        var existingNames = existingApplicationRoles.Select(role => role.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rolesToAdd = targetRoles.Where(role => !existingNames.Contains(role.Name)).ToList();
        var ok = await _keycloakAdmin.AssignRealmRolesToUserAsync(
            userId,
            rolesToAdd.Select(role => new KeycloakAdminService.KeycloakRolePublic(role.Id.ToString(), role.Name)));

        if (!ok)
            return StatusCode(502, new ProblemDetails { Title = "Pristupi nisu sačuvani", Detail = "Keycloak trenutno nije prihvatio izmjenu. Pokušajte ponovo." });

        var targetNamesSet = targetRoles.Select(role => role.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rolesToRemove = existingApplicationRoles.Where(role => !targetNamesSet.Contains(role.Name)).ToList();
        if (rolesToRemove.Count > 0 && !await _keycloakAdmin.RemoveRealmRolesFromUserAsync(userId, rolesToRemove))
            return StatusCode(502, new ProblemDetails { Title = "Pristupi nisu potpuno ažurirani", Detail = "Novi pristupi su dodani, ali stari nisu uklonjeni. Pokušajte ponovo ili kontaktirajte podršku." });

        var localUserForKeycloak = await _db.AppUsers.FirstOrDefaultAsync(user => user.KeycloakId == userId);
        if (localUserForKeycloak is not null)
        {
            var targetNames = targetRoles.Select(role => role.Name).ToArray();
            var localRoleIds = await _db.Roles
                .Where(role => targetNames.Contains(role.Name))
                .Select(role => role.Id)
                .ToListAsync();
            await ReplaceLocalRoles(localUserForKeycloak.Id, localRoleIds);
            await _db.SaveChangesAsync();
        }

        await _audit.LogAsync(new AuditEntry
        {
            TableName = "AppUser", RecordId = userId, Action = "ROLE_ASSIGN",
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { Roles = targetRoles.Select(role => role.Name) }),
            UserId = CurrentUsername(), Username = CurrentUsername()
        });

        return Ok(new { message = "Pristupi korisnika su uspješno ažurirani.", roles = targetRoles.Select(role => role.Name) });
    }

    private async Task ReplaceLocalRoles(Guid userId, IReadOnlyCollection<Guid> roleIds)
    {
        var existing = await _db.UserRoles.Where(item => item.UserId == userId).ToListAsync();
        foreach (var item in existing)
            item.IsActive = roleIds.Contains(item.RoleId);

        var existingRoleIds = existing.Select(item => item.RoleId).ToHashSet();
        foreach (var roleId in roleIds.Where(roleId => !existingRoleIds.Contains(roleId)))
            _db.UserRoles.Add(new DL.Entities.Role.UserRole { UserId = userId, RoleId = roleId, CreatedBy = CurrentUsername() });
    }

    /// <summary>Soft-deactivates a user (IsActive=false + Keycloak disabled).</summary>
    [HttpDelete("{userId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> DeactivateUser(
        [FromRoute] string userId)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;
        if (userId == currentUserId)
            return BadRequest(new { errors = new[] { new { field = (string?)null, message = "Ne možete deaktivirati vlastiti nalog." } } });

        if (!_keycloakAdmin.IsEnabled && Guid.TryParse(userId, out var localId))
        {
            var localUser = await _db.AppUsers.FindAsync(localId);
            if (localUser is null) return NotFound(new ProblemDetails { Title = "Korisnik nije pronađen" });
            localUser.IsActive = false;
            localUser.ModifiedAt = DateTime.UtcNow;
            localUser.ModifiedBy = CurrentUsername();
            await _db.SaveChangesAsync();
            return Ok(new { message = "Korisnik je lokalno deaktiviran." });
        }
        var ok = await _keycloakAdmin.SetUserEnabledAsync(userId, false);
        if (!ok)
            return NotFound(new { errors = new[] { new { field = (string?)null, message = "Korisnik nije pronađen." } } });

        await _audit.LogAsync(new AuditEntry
        {
            TableName = "AppUser", RecordId = userId, Action = "DEACTIVATE",
            OldValues = System.Text.Json.JsonSerializer.Serialize(new { IsActive = true }),
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { IsActive = false }),
            UserId = CurrentUsername(), Username = CurrentUsername()
        });
        return Ok(new { message = "Korisnik uspješno deaktiviran." });
    }

    /// <summary>Reactivates a previously deactivated user.</summary>
    [HttpPost("{userId}/reactivate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> ReactivateUser([FromRoute] string userId)
    {
        if (!_keycloakAdmin.IsEnabled && Guid.TryParse(userId, out var localId))
        {
            var localUser = await _db.AppUsers.IgnoreQueryFilters().FirstOrDefaultAsync(user => user.Id == localId);
            if (localUser is null) return NotFound(new ProblemDetails { Title = "Korisnik nije pronađen" });
            localUser.IsActive = true;
            localUser.ModifiedAt = DateTime.UtcNow;
            localUser.ModifiedBy = CurrentUsername();
            await _db.SaveChangesAsync();
            return Ok(new { message = "Korisnik je lokalno reaktiviran." });
        }
        var ok = await _keycloakAdmin.SetUserEnabledAsync(userId, true);
        if (!ok)
            return NotFound(new { errors = new[] { new { field = (string?)null, message = "Korisnik nije pronađen." } } });

        await _audit.LogAsync(new AuditEntry
        {
            TableName = "AppUser", RecordId = userId, Action = "REACTIVATE",
            OldValues = System.Text.Json.JsonSerializer.Serialize(new { IsActive = false }),
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { IsActive = true }),
            UserId = CurrentUsername(), Username = CurrentUsername()
        });
        return Ok(new { message = "Korisnik uspješno reaktiviran." });
    }

    /// <summary>Permanently removes a user after explicit confirmation in the UI.</summary>
    [HttpDelete("{userId}/permanent")]
    public async Task<ActionResult<object>> DeleteUser([FromRoute] string userId)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;
        if (userId == currentUserId)
            return BadRequest(new ProblemDetails { Title = "Brisanje nije dozvoljeno", Detail = "Ne možete obrisati vlastiti nalog." });

        AppUser? localUser;
        if (_keycloakAdmin.IsEnabled)
        {
            if (!await _keycloakAdmin.DeleteUserAsync(userId))
                return NotFound(new ProblemDetails { Title = "Korisnik nije pronađen", Detail = "Korisnik nije pronađen u Keycloaku." });
            localUser = await _db.AppUsers.FirstOrDefaultAsync(user => user.KeycloakId == userId);
        }
        else
        {
            if (!Guid.TryParse(userId, out var localId))
                return BadRequest(new ProblemDetails { Title = "Neispravan korisnik" });
            localUser = await _db.AppUsers.FindAsync(localId);
            if (localUser is null)
                return NotFound(new ProblemDetails { Title = "Korisnik nije pronađen" });
        }

        if (localUser is not null)
        {
            var assignments = await _db.UserRoles.Where(role => role.UserId == localUser.Id).ToListAsync();
            _db.UserRoles.RemoveRange(assignments);
            _db.AppUsers.Remove(localUser);
            await _db.SaveChangesAsync();
        }
        await _audit.LogAsync(new AuditEntry { TableName = "AppUser", RecordId = userId, Action = "DELETE", UserId = CurrentUsername(), Username = CurrentUsername() });
        return Ok(new { message = "Korisnik je obrisan." });
    }
}
