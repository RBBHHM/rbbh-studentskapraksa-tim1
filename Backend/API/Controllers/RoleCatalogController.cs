using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.Roles;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

[ApiController, Route("api/role-catalog"), Authorize(Policy = ApplicationPolicies.AdministrationRead)]
public sealed class RoleCatalogController(RoleCatalogService catalog) : ControllerBase
{
    [HttpGet]
    public Task<object> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, CancellationToken ct = default) => catalog.List(page, pageSize, search, ct);

    [HttpGet("{id:long}")]
    public Task<RoleCatalogDTO> Get(long id, CancellationToken ct) => catalog.Get(id, ct);

    [HttpGet("permissions")]
    public Task<object> Permissions(CancellationToken ct) => catalog.Permissions(ct);

    [HttpPost, Authorize(Policy = ApplicationPolicies.AdminOnly)]
    public async Task<IActionResult> Create(CreateRoleCatalogDTO input, CancellationToken ct)
    {
        var row = await catalog.Create(input, ct);
        if (row is null)
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: Localization.BackendMessages.Message("errors.roles.duplicate.name"),
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "roles.duplicate_name",
                    ["resourceKey"] = "errors.roles.duplicate.name"
                });
        return Created($"/api/role-catalog/{row.Id}", row);
    }

    [HttpDelete("{id:long}"), Authorize(Policy = ApplicationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(long id, [FromQuery] DateTime? expectedVersion, CancellationToken ct)
    {
        if (expectedVersion is null) return BadRequest();
        return (await catalog.Delete(id, expectedVersion.Value, ct)) switch
        {
            RoleCatalogService.RoleDeletionResult.Deleted => NoContent(),
            RoleCatalogService.RoleDeletionResult.Missing => NotFound(),
            RoleCatalogService.RoleDeletionResult.Protected => Conflict(new { code = "roles.protected" }),
            RoleCatalogService.RoleDeletionResult.InUse => Conflict(new { code = "roles.in_use" }),
            _ => Conflict(new { code = "request.concurrent_change" })
        };
    }

    [HttpPut("{id:long}"), Authorize(Policy = ApplicationPolicies.AdminOnly)]
    public Task<RoleCatalogDTO> Update(long id, UpdateRoleCatalogDTO input, CancellationToken ct) => catalog.Update(id, input, ct);
}
