using RBBH.ConnectedParties.API.Controllers.BaseController;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Report;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RBBH.ConnectedParties.Helpers.Excel;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

/// <summary>
/// API za kapital banke s historijom po datumu važenja.
/// </summary>
[RBBH.ConnectedParties.Helpers.RequireActorEmail]
[ApiController]
[Route("api/capital")]
[Authorize]
public class CapitalController(ICapitalService capitalService, IAuditService auditService) : BaseResuItController
{
    private readonly ICapitalService _capitalService = capitalService;
    private readonly IAuditService _auditService = auditService;

    /// <summary>Vraća poznati kapital koji važi danas, ili nema sadržaja ako nije poznat.</summary>
    [HttpGet]
    [Authorize(Policy = ApplicationPolicies.CapitalRead)]
    [ProducesResponseType(typeof(CapitalDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<CapitalDTO?>> Get()
    {
        var result = await _capitalService.Get();
        return HandleResult(result);
    }

    [HttpGet("as-of"), Authorize(Policy = ApplicationPolicies.CapitalRead)]
    public async Task<ActionResult<CapitalDTO?>> AsOf([FromQuery] DateTime date, CancellationToken ct)
    {
        if (!Request.Query.ContainsKey("date")) return BadRequest(new ProblemDetails { Status = 400, Title = BackendMessages.Message("errors.capital.date.required") });
        return HandleResult(await _capitalService.AsOf(date, ct));
    }

    [HttpGet("history"), Authorize(Policy = ApplicationPolicies.CapitalRead)]
    public Task<BL.Services.ReferencePage<CapitalHistoryDTO>> History(CancellationToken ct, int page = 1, int pageSize = 20) =>
        _capitalService.History(page, pageSize, ct);

    /// <summary>Izvoz kapitala u Excel — samo 4 kolone kapitala.</summary>
    [HttpGet("export")]
    [Authorize(Policy = ApplicationPolicies.CapitalRead)]
    public async Task<IActionResult> Export()
    {
        var result = await _capitalService.Get();
        if (!result.IsSuccessful) return HTTPExceptiontFromResult(result).Result!;
        var rows = result.Value is null
            ? []
            : new[] { (IReadOnlyList<object?>)[result.Value.CoreCapital, result.Value.RegulatoryCapital, result.Value.SupplementaryCapital, result.Value.CapitalDate] };
        var bytes = RegistryExcelExporter.Create(
            "Kapital",
            ["Osnovni kapital", "Regulatorni kapital", "Dopunski kapital", "Datum kapitala"],
            rows);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"kapital-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx");
    }

    /// <summary>Dodaje nepromjenjivu reviziju kapitala (Regulatorni/Osnovni/Dopunski + datum).</summary>
    [HttpPut]
    [Authorize(Policy = ApplicationPolicies.CapitalEdit)]
    [ProducesResponseType(typeof(CapitalDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CapitalDTO>> Upsert([FromBody] UpdateCapitalDTO dto)
    {
        var korisnik = GetKorisnik();
        var result = await _capitalService.Upsert(dto, korisnik);
        return HandleResult(result);
    }

    // ─── Helper ─────────────────────────────────────────────────────────────

    private string GetKorisnik()
    {
        return RBBH.ConnectedParties.Helpers.AuthenticatedActor.Username(User);
    }
}
