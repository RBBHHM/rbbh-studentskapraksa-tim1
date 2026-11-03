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

    [HttpPut("{userId}")]
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    public async Task<ActionResult<object>> UpdateProfile(string userId, UpdateUserProfileDTO dto) =>
        MutationResponse(await _appUserService.UpdateProfileAsync(userId, dto,
            RBBH.ConnectedParties.Helpers.AuthenticatedActor.Capture(User), RemoteIp, HttpContext.RequestAborted),
            BackendMessages.Message("messages.users.profile.updated"));

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
            BackendMessages.Message("messages.users.roles.updated"));

    /// <summary>Soft-deactivates the application privilege mapping.</summary>
    [HttpDelete("{userId}")]
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> DeactivateUser([FromRoute] string userId) =>
        MutationResponse(await _appUserService.DeactivateAsync(userId,
            RBBH.ConnectedParties.Helpers.AuthenticatedActor.Capture(User), RemoteIp, HttpContext.RequestAborted),
            BackendMessages.Message("messages.users.deactivated"));

    /// <summary>Reactivates a previously deactivated user.</summary>
    [HttpPost("{userId}/reactivate")]
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> ReactivateUser([FromRoute] string userId) =>
        MutationResponse(await _appUserService.ReactivateAsync(userId,
            RBBH.ConnectedParties.Helpers.AuthenticatedActor.Capture(User), RemoteIp, HttpContext.RequestAborted),
            BackendMessages.Message("messages.users.reactivated"));

    /// <summary>Permanently removes a user after explicit confirmation in the UI.</summary>
    [HttpDelete("{userId}/permanent")]
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    public async Task<ActionResult<object>> DeleteUser([FromRoute] string userId) =>
        MutationResponse(await _appUserService.DeleteAsync(userId,
            RBBH.ConnectedParties.Helpers.AuthenticatedActor.Capture(User), RemoteIp, HttpContext.RequestAborted),
            BackendMessages.Message("messages.users.deleted"));

    private string? RemoteIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    private ActionResult<object> MutationResponse(UserMutationResult result, string message) => result.Status switch
    {
        UserMutationStatus.Success => result.Roles is { } roles ? Ok(new { message, roles }) : Ok(new { message }),
        UserMutationStatus.NotFound => NotFound(new ProblemDetails { Title = BackendMessages.Message("errors.users.not.found.title") }),
        UserMutationStatus.InvalidRoles => BadRequest(new ProblemDetails
        {
            Title = BackendMessages.Message("errors.users.invalid.role.title"),
            Detail = BackendMessages.Message("errors.users.invalid.role.detail")
        }),
        UserMutationStatus.FinalAdmin => Conflict(new ProblemDetails
        {
            Title = BackendMessages.Message("errors.users.change.denied.title"),
            Detail = BackendMessages.Message("errors.users.final.admin")
        }),
        UserMutationStatus.SelfRoles => BadRequest(new ProblemDetails
        {
            Title = BackendMessages.Message("errors.users.change.denied.title"),
            Detail = BackendMessages.Message("errors.users.self.roles.denied")
        }),
        UserMutationStatus.SelfDeactivate => BadRequest(new
        {
            errors = new[] { new { field = (string?)null, message = BackendMessages.Message("errors.users.self.deactivate.denied") } }
        }),
        UserMutationStatus.SelfDelete => BadRequest(new ProblemDetails
        {
            Title = BackendMessages.Message("errors.users.delete.denied.title"),
            Detail = BackendMessages.Message("errors.users.self.delete.denied")
        }),
        UserMutationStatus.Busy => Conflict(new ProblemDetails
        {
            Title = BackendMessages.Message("errors.users.busy.title"),
            Detail = BackendMessages.Message("errors.users.busy.detail")
        }),
        _ => throw new InvalidOperationException("Unknown user mutation outcome.")
    };
}
