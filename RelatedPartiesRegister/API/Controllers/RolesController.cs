using RBBH.ConnectedParties.API.Controllers.BaseController;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = "application-administration")]
public class RolesController(IRoleService roleService, KeycloakAdminService keycloakAdmin) : BaseResuItController
{
    /// <summary>
    /// Returns all realm roles from Keycloak (source of truth).
    /// Falls back to local DB if Keycloak is unreachable.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetRolesResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetRolesResponseDTO>> GetAllRoles()
    {
        var kcRoles = await keycloakAdmin.GetRealmRolesAsync();
        if (kcRoles.IsSuccessful && kcRoles.Value.Any())
            return Ok(new GetRolesResponseDTO
            {
                Roles = kcRoles.Value.Where(role => ApplicationAccessRoles.All.Contains(role.Name)).ToList()
            });

        // Fallback: local DB
        var result = await roleService.GetAllRoles();
        return HandleResult(result);
    }
}
