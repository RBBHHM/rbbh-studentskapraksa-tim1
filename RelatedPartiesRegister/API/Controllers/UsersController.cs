using RBBH.ConnectedParties.API.Controllers.BaseController;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Roles;
using RBBH.ConnectedParties.DL.DTO.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

/// <summary>
/// US2: Upravljanje korisnicima — lista, kreiranje, dodjela rola.
/// </summary>
[RBBH.ConnectedParties.Helpers.RequireActorEmail]
[ApiController]
[Route("api/users")]
[Authorize(Policy = ApplicationPolicies.AdministrationRead)]
public class UsersController(IAppUserService appUserService)
    : BaseResuItController
{
    private readonly IAppUserService _appUserService = appUserService;
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
        var createdBy = RBBH.ConnectedParties.Helpers.AuthenticatedActor.Email(User);

        var result = await _appUserService.CreateUserAsync(dto, createdBy, HttpContext.Connection.RemoteIpAddress?.ToString());

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
        [FromRoute] string userId, [FromBody] AssignRoleRequestDTO request) =>
        MutationResponse(await _appUserService.ReplaceRolesAsync(userId, request.EffectiveRoleIds,
            RBBH.ConnectedParties.Helpers.AuthenticatedActor.Capture(User), RemoteIp, HttpContext.RequestAborted),
            "Role korisnika su uspješno ažurirane.");

    /// <summary>Soft-deactivates the application privilege mapping.</summary>
    [HttpDelete("{userId}")]
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> DeactivateUser([FromRoute] string userId) =>
        MutationResponse(await _appUserService.DeactivateAsync(userId,
            RBBH.ConnectedParties.Helpers.AuthenticatedActor.Capture(User), RemoteIp, HttpContext.RequestAborted),
            "Korisnik uspješno deaktiviran.");

    /// <summary>Reactivates a previously deactivated user.</summary>
    [HttpPost("{userId}/reactivate")]
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> ReactivateUser([FromRoute] string userId) =>
        MutationResponse(await _appUserService.ReactivateAsync(userId,
            RBBH.ConnectedParties.Helpers.AuthenticatedActor.Capture(User), RemoteIp, HttpContext.RequestAborted),
            "Korisnik uspješno reaktiviran.");

    /// <summary>Permanently removes a user after explicit confirmation in the UI.</summary>
    [HttpDelete("{userId}/permanent")]
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    public async Task<ActionResult<object>> DeleteUser([FromRoute] string userId) =>
        MutationResponse(await _appUserService.DeleteAsync(userId,
            RBBH.ConnectedParties.Helpers.AuthenticatedActor.Capture(User), RemoteIp, HttpContext.RequestAborted),
            "Korisnik je obrisan.");

    private string? RemoteIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    private ActionResult<object> MutationResponse(UserMutationResult result, string message) => result.Status switch
    {
        UserMutationStatus.Success => result.Roles is { } roles ? Ok(new { message, roles }) : Ok(new { message }),
        UserMutationStatus.NotFound => NotFound(new ProblemDetails { Title = "Korisnik nije pronađen" }),
        UserMutationStatus.InvalidRoles => BadRequest(new ProblemDetails
        {
            Title = "Neispravna rola",
            Detail = "Jedna ili više odabranih aplikacijskih rola nije ispravna."
        }),
        UserMutationStatus.FinalAdmin => Conflict(new ProblemDetails
        {
            Title = "Promjena nije dozvoljena",
            Detail = "U sistemu mora ostati najmanje jedan aktivni Admin korisnik."
        }),
        UserMutationStatus.SelfRoles => BadRequest(new ProblemDetails
        {
            Title = "Promjena nije dozvoljena",
            Detail = "Ne možete mijenjati vlastite aplikacijske role."
        }),
        UserMutationStatus.SelfDeactivate => BadRequest(new
        {
            errors = new[] { new { field = (string?)null, message = "Ne možete deaktivirati vlastiti nalog." } }
        }),
        UserMutationStatus.SelfDelete => BadRequest(new ProblemDetails
        {
            Title = "Brisanje nije dozvoljeno",
            Detail = "Ne možete obrisati vlastiti nalog."
        }),
        UserMutationStatus.Busy => Conflict(new ProblemDetails
        {
            Title = "Promjena trenutno nije moguća",
            Detail = "Upravljanje korisnicima je zauzeto. Osvježite podatke i pokušajte ponovo."
        }),
        _ => throw new InvalidOperationException("Unknown user mutation outcome.")
    };
}
