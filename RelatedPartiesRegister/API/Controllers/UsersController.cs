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
[Authorize(Policy = ApplicationPolicies.AdministrationRead)]
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
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
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
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> AssignOrUpdateUserRole(
        [FromRoute] string userId,
        [FromBody] AssignRoleRequestDTO request)
    {
        var roleIds = request.EffectiveRoleIds;
        var localRoles = await _db.Roles
            .Where(role => roleIds.Contains(role.Id) && role.IsActive)
            .ToListAsync();
        if (localRoles.Count != roleIds.Count || localRoles.Any(role => !ApplicationAccessRoles.Assignable.Contains(role.Name)))
            return BadRequest(new ProblemDetails { Title = "Neispravna rola", Detail = "Jedna ili više odabranih aplikacijskih rola nije ispravna." });

        AppUser? localUser = null;
        if (Guid.TryParse(userId, out var localUserId))
            localUser = await _db.AppUsers.FindAsync(localUserId);
        localUser ??= await _db.AppUsers.FirstOrDefaultAsync(user => user.KeycloakId == userId);
        if (localUser is null)
            return NotFound(new ProblemDetails { Title = "Korisnik nije pronađen" });

        if (roleIds.Count == 0)
        {
            var assignments = await _db.UserRoles.Where(item => item.UserId == localUser.Id).ToListAsync();
            _db.UserRoles.RemoveRange(assignments);
            _db.AppUsers.Remove(localUser);
        }
        else
            await ReplaceLocalRoles(localUser.Id, roleIds);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(new AuditEntry
        {
            TableName = "AppUser", RecordId = userId, Action = "ROLE_ASSIGN",
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { Roles = localRoles.Select(role => role.Name) }),
            UserId = CurrentUsername(), Username = CurrentUsername()
        });

        return Ok(new { message = "Role korisnika su uspješno ažurirane.", roles = localRoles.Select(role => role.Name) });
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

    /// <summary>Soft-deactivates the application privilege mapping.</summary>
    [HttpDelete("{userId}")]
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
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

        AppUser? localUser = Guid.TryParse(userId, out var localId)
            ? await _db.AppUsers.FindAsync(localId)
            : await _db.AppUsers.FirstOrDefaultAsync(user => user.KeycloakId == userId);
        if (localUser is null) return NotFound(new ProblemDetails { Title = "Korisnik nije pronađen" });
        localUser.IsActive = false;
        localUser.ModifiedAt = DateTime.UtcNow;
        localUser.ModifiedBy = CurrentUsername();
        await _db.SaveChangesAsync();

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
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> ReactivateUser([FromRoute] string userId)
    {
        AppUser? localUser = Guid.TryParse(userId, out var localId)
            ? await _db.AppUsers.IgnoreQueryFilters().FirstOrDefaultAsync(user => user.Id == localId)
            : await _db.AppUsers.IgnoreQueryFilters().FirstOrDefaultAsync(user => user.KeycloakId == userId);
        if (localUser is null) return NotFound(new ProblemDetails { Title = "Korisnik nije pronađen" });
        localUser.IsActive = true;
        localUser.ModifiedAt = DateTime.UtcNow;
        localUser.ModifiedBy = CurrentUsername();
        await _db.SaveChangesAsync();

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
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    public async Task<ActionResult<object>> DeleteUser([FromRoute] string userId)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;
        if (userId == currentUserId)
            return BadRequest(new ProblemDetails { Title = "Brisanje nije dozvoljeno", Detail = "Ne možete obrisati vlastiti nalog." });

        AppUser? localUser = Guid.TryParse(userId, out var localId)
            ? await _db.AppUsers.FindAsync(localId)
            : await _db.AppUsers.FirstOrDefaultAsync(user => user.KeycloakId == userId);
        if (localUser is null)
            return NotFound(new ProblemDetails { Title = "Korisnik nije pronađen" });

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
