using RBBH.ConnectedParties.API.Controllers.BaseController;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Limiti;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RBBH.ConnectedParties.Helpers.Excel;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

/// <summary>
/// CRUD API za upravljanje limitima.
/// </summary>
[ApiController]
[Route("api/limiti")]
[Authorize]
public class LimitController(ILimitService limitService) : BaseResuItController
{
    private readonly ILimitService _limitService = limitService;

    /// <summary>Vraća sve limite.</summary>
    [HttpGet]
    [Authorize(Policy = ApplicationPolicies.LimitsRead)]
    [ProducesResponseType(typeof(List<LimitResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LimitResponseDTO>>> GetAll()
    {
        var result = await _limitService.GetAll();
        return HandleResult(result);
    }

    [HttpGet("export")]
    [Authorize(Policy = ApplicationPolicies.LimitsRead)]
    public async Task<IActionResult> Export([FromQuery] string? identifier = null)
    {
        var result = await _limitService.GetAll();
        if (!result.IsSuccessful) return HTTPExceptiontFromResult(result).Result!;
        var items = result.Value.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(identifier))
        {
            var term = identifier.Trim();
            items = items.Where(item => string.Equals(item.MaticniBroj, term, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.TaxNumber, term, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.FbaId, term, StringComparison.OrdinalIgnoreCase));
        }
        var bytes = RegistryExcelExporter.Create(
            "Limiti",
            ["Redni broj", "Rezidentnost", "FBA_ID", "Porez_broj", "Matbroj/JMBG", "Naziv", "Tip limita", "Odobreni limit", "Maksimalna očekivana utilizacija", "Rok očekivane utilizacije", "Komentar", "Regulatorni kapital", "Osnovni kapital", "Datum kapitala", "Datum izmjene", "user_verified"],
            items.Select((item, index) => (IReadOnlyList<object?>)
            [index + 1, item.IsResident is null ? null : item.IsResident.Value ? "Rezident" : "Nerezident", item.FbaId, item.TaxNumber, item.MaticniBroj, item.Naziv, item.TipLimita, item.IznosLimita, item.MaksimalnoOcekivanaUtilizacija, item.RokUtilizacije, item.Komentar, item.RegulatorniKapital, item.OsnovniKapital, item.DatumKapitala, item.ModifiedAt ?? item.CreatedAt, item.ModifiedBy]));
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"limiti-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx");
    }

    /// <summary>Vraća jedan limit po ID-u.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = ApplicationPolicies.LimitsRead)]
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
    [Authorize(Policy = ApplicationPolicies.LimitsCreate)]
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
    [Authorize(Policy = ApplicationPolicies.LimitsEdit)]
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
    [Authorize(Policy = ApplicationPolicies.LimitsDelete)]
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
