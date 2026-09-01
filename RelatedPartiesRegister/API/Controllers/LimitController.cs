using RBBH.ConnectedParties.API.Controllers.BaseController;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Limiti;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RBBH.ConnectedParties.Helpers.Excel;

namespace RBBH.ConnectedParties.API.Controllers;

/// <summary>
/// CRUD API za upravljanje limitima.
/// </summary>
[ApiController]
[Route("api/limiti")]
[Authorize(Roles = "limits")]
public class LimitController(ILimitService limitService) : BaseResuItController
{
    private readonly ILimitService _limitService = limitService;

    /// <summary>Vraća sve limite.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<LimitResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LimitResponseDTO>>> GetAll()
    {
        var result = await _limitService.GetAll();
        return HandleResult(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var result = await _limitService.GetAll();
        if (!result.IsSuccessful) return HTTPExceptiontFromResult(result).Result!;
        var bytes = RegistryExcelExporter.Create(
            "Limiti",
            ["Naziv", "Tip limita", "Iznos limita", "Utilizacija", "Korigovani limit", "Raspoloživi limit", "Rok utilizacije", "Komentar", "Regulatorni kapital", "Osnovni kapital"],
            result.Value.Select(item => (IReadOnlyList<object?>)
            [item.Naziv, item.TipLimita, item.IznosLimita, item.Utilizacija, item.KorigovaniLimit, item.RaspoloziviLimit, item.RokUtilizacije, item.Komentar, item.RegulatorniKapital, item.OsnovniKapital]));
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"limiti-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx");
    }

    [HttpPut("{id:int}/capital")]
    public async Task<ActionResult<LimitResponseDTO>> UpdateCapital([FromRoute] int id, [FromBody] UpdateCapitalDTO dto)
        => HandleResult(await _limitService.UpdateCapital(id, dto, GetKorisnik()));

    /// <summary>Vraća jedan limit po ID-u.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LimitResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LimitResponseDTO>> GetByID([FromRoute] int id)
    {
        var result = await _limitService.GetByID(id);
        return HandleResult(result);
    }

    /// <summary>Kreira novi limit.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LimitResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LimitResponseDTO>> Create([FromBody] CreateLimitDTO dto)
    {
        var korisnik = GetKorisnik();
        var result = await _limitService.Create(dto, korisnik);

        if (result.IsSuccessful)
            return Created($"{Request.Path}/{result.Value.Id}", result.Value);

        return HTTPExceptiontFromResult(result);
    }

    /// <summary>Ažurira postojeći limit.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(LimitResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LimitResponseDTO>> Update([FromRoute] int id, [FromBody] UpdateLimitDTO dto)
    {
        var korisnik = GetKorisnik();
        var result = await _limitService.Update(id, dto, korisnik);
        return HandleResult(result);
    }

    /// <summary>Briše limit.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> Delete([FromRoute] int id)
    {
        var result = await _limitService.Delete(id);

        if (result.IsSuccessful)
            return Ok(new { message = "Limit je uspješno obrisan." });

        return HTTPExceptiontFromResult(result);
    }

    // ─── Helper ─────────────────────────────────────────────────────────────

    private string GetKorisnik()
    {
        return User.Identity?.Name
            ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
            ?? User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
            ?? Environment.MachineName;
    }
}
