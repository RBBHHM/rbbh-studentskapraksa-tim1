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
[RBBH.ConnectedParties.Helpers.RequireActorEmail]
[ApiController]
[Route("api/limiti")]
[Authorize]
public class LimitController(ILimitService limitService, IAuditService auditService) : BaseResuItController
{
    private readonly ILimitService _limitService = limitService;
    private readonly IAuditService _auditService = auditService;

    /// <summary>Vraća sve limite, opcionalno filtrirano po pravnom licu ili matičnom broju.</summary>
    [HttpGet]
    [Authorize(Policy = ApplicationPolicies.LimitsRead)]
    [ProducesResponseType(typeof(List<LimitResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LimitResponseDTO>>> GetAll([FromQuery] Guid? legalEntityId = null, [FromQuery] string? matbroj = null)
    {
        var result = await _limitService.GetAll(legalEntityId, matbroj);
        return HandleResult(result);
    }

    /// <summary>
    /// Izvoz limita u Excel. Ako je zadan legalEntityId/matbroj, izvozi samo tog klijenta;
    /// u suprotnom izvozi sve klijente koji imaju limite, sa svim podacima iz Regulatornog izvještavanja.
    /// </summary>
    [HttpGet("export")]
    [Authorize(Policy = ApplicationPolicies.LimitsRead)]
    public async Task<IActionResult> Export([FromQuery] Guid? legalEntityId = null, [FromQuery] string? matbroj = null)
    {
        var result = await _limitService.GetAll(legalEntityId, matbroj);
        if (!result.IsSuccessful) return HTTPExceptiontFromResult(result).Result!;
        var bytes = RegistryExcelExporter.Create(
            "Limiti",
            ["Naziv", "Rezidentnost", "FBA ID", "Porezni broj", "Matični broj", "Matbroj", "Tip limita", "Odobreni limit", "Maksimalna očekivana utilizacija", "Rok očekivane utilizacije", "Komentar", "Regulatorni kapital", "Osnovni kapital", "Datum kapitala"],
            result.Value.Select(item => (IReadOnlyList<object?>)
            [item.Naziv, item.IsResident ? "Rezident" : "Nerezident", item.FbaId, item.TaxNumber, item.MaticniBroj, item.Matbroj, item.TipLimita, item.OdobreniLimit, item.MaksimalnaOcekivanaUtilizacija, item.RokOcekivaneUtilizacije, item.Komentar, item.RegulatorniKapital, item.OsnovniKapital, item.DatumKapitala]));
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
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TableName = "Limit",
                RecordId = result.Value.Id.ToString(),
                Action = "INSERT",
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    naziv = result.Value.Naziv,
                    tipLimita = result.Value.TipLimita,
                    odobreniLimit = result.Value.OdobreniLimit
                }),
                UserId = korisnik,
                Username = korisnik,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            return Created($"{Request.Path}/{result.Value.Id}", result.Value);
        }

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

        if (result.IsSuccessful)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TableName = "Limit",
                RecordId = id.ToString(),
                Action = "UPDATE",
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    naziv = result.Value.Naziv,
                    tipLimita = result.Value.TipLimita,
                    odobreniLimit = result.Value.OdobreniLimit
                }),
                UserId = korisnik,
                Username = korisnik,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
        }

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
        {
            var korisnik = GetKorisnik();
            await _auditService.LogAsync(new AuditEntry
            {
                TableName = "Limit",
                RecordId = id.ToString(),
                Action = "DELETE",
                UserId = korisnik,
                Username = korisnik,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            return Ok(new { message = "Limit je uspješno obrisan." });
        }

        return HTTPExceptiontFromResult(result);
    }

    // ─── Helper ─────────────────────────────────────────────────────────────

    private string GetKorisnik()
    {
        return RBBH.ConnectedParties.Helpers.AuthenticatedActor.Email(User);
    }
}
