using RBBH.ConnectedParties.API.Controllers.BaseController;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = ApplicationPolicies.AdministrationRead)]
public class RolesController(IRoleService roleService) : BaseResuItController
{
    /// <summary>Returns the local application role catalog.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetRolesResponseDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetRolesResponseDTO>> GetAllRoles()
    {
        var result = await roleService.GetAllRoles();
        return HandleResult(result);
    }
}
