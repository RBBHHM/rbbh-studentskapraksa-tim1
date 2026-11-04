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
    public async Task<IActionResult> Submit(CreateRoleRequestDTO input, CancellationToken ct)
    {
        var (request, created) = await service.Submit(input, ct);
        return created ? Created("/api/role-requests/mine", request) : Ok(request);
    }

    [HttpGet("mine")]
    public Task<PagedResponse<RoleRequestDTO>> Mine([FromQuery] string? status = null, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default) => service.List(false, status, page, pageSize, ct);

    [HttpGet, Authorize(Policy = ApplicationPolicies.AdminOnly)]
    public Task<PagedResponse<RoleRequestDTO>> Review([FromQuery] string? status = null, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default) => service.List(true, status, page, pageSize, ct);

    [HttpPost("{id:guid}/approve"), Authorize(Policy = ApplicationPolicies.AdminOnly)]
    public Task<RoleRequestDTO> Approve(Guid id, RoleRequestDecisionDTO input, CancellationToken ct) => service.Decide(id, true, input, ct);

    [HttpPost("{id:guid}/reject"), Authorize(Policy = ApplicationPolicies.AdminOnly)]
    public Task<RoleRequestDTO> Reject(Guid id, RoleRequestDecisionDTO input, CancellationToken ct) => service.Decide(id, false, input, ct);
}
