using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.Roles;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

[ApiController, Route("api/role-requests"), Authorize(Policy = ApplicationPolicies.ApplicationAccess)]
public sealed class RoleRequestsController(RoleRequestService service) : ControllerBase
{
    [HttpGet("options")]
    public Task<object> Options(CancellationToken ct) => service.Options(ct);

    [HttpPost]
    public async Task<IActionResult> Submit(CreateRoleRequestDTO input, CancellationToken ct) =>
        MutationResponse(await service.Submit(input, ct));

    [HttpGet("mine")]
    public Task<PagedResponse<RoleRequestDTO>> Mine([FromQuery] string? status = null, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] long? requestId = null, CancellationToken ct = default) =>
        service.List(false, status, page, pageSize, ct, requestId);

    [HttpGet, Authorize(Policy = ApplicationPolicies.AdminOnly)]
    public async Task<IActionResult> Review([FromQuery] string? status = null, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, [FromQuery] long? requestId = null, CancellationToken ct = default) =>
        await service.CanReview(ct) ? Ok(await service.List(true, status, page, pageSize, ct, requestId)) : Forbid();

    [HttpPost("{id:long}/approve"), Authorize(Policy = ApplicationPolicies.AdminOnly)]
    public async Task<IActionResult> Approve(long id, RoleRequestDecisionDTO input, CancellationToken ct) =>
        MutationResponse(await service.Decide(id, true, input, ct));

    [HttpPost("{id:long}/reject"), Authorize(Policy = ApplicationPolicies.AdminOnly)]
    public async Task<IActionResult> Reject(long id, RoleRequestDecisionDTO input, CancellationToken ct) =>
        MutationResponse(await service.Decide(id, false, input, ct));

    private IActionResult MutationResponse(RoleRequestResult result)
    {
        if (result.Code is { } code)
            return Problem(statusCode: result.Status, extensions: new Dictionary<string, object?> { ["code"] = code });
        return result.Status == StatusCodes.Status201Created
            ? Created("/api/role-requests/mine", result.Request)
            : Ok(result.Request);
    }
}
