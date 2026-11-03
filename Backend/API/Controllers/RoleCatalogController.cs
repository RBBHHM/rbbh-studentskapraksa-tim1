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

    [HttpGet("{id:guid}")]
    public Task<RoleCatalogDTO> Get(Guid id, CancellationToken ct) => catalog.Get(id, ct);

    [HttpGet("permissions")]
    public Task<object> Permissions(CancellationToken ct) => catalog.Permissions(ct);

    [HttpPost, Authorize(Policy = ApplicationPolicies.AdminOnly)]
    public async Task<IActionResult> Create(CreateRoleCatalogDTO input, CancellationToken ct)
    {
        var row = await catalog.Create(input, ct);
        return Created($"/api/role-catalog/{row.Id}", row);
    }

    [HttpPut("{id:guid}"), Authorize(Policy = ApplicationPolicies.AdminOnly)]
    public Task<RoleCatalogDTO> Update(Guid id, UpdateRoleCatalogDTO input, CancellationToken ct) => catalog.Update(id, input, ct);
}
