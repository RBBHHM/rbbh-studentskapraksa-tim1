using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBBH.ConnectedParties.API.Controllers.BaseController;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Sifarnici;

namespace RBBH.ConnectedParties.API.Controllers;

/// <summary>Read-only reference data used by authenticated application forms.</summary>
[ApiController]
[Route("api/code-lists/dropdown")]
[Authorize]
public sealed class ReferenceDataController(ICodeListService codeListService) : BaseResuItController
{
    [HttpGet("{kategorija}")]
    [ProducesResponseType(typeof(List<CodeListDropdownDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<CodeListDropdownDTO>>> GetDropdown([FromRoute] string kategorija)
    {
        var result = await codeListService.GetDropdownByKategorija(kategorija);
        return HandleResult(result);
    }
}
