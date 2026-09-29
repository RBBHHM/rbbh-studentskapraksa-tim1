using RBBH.ConnectedParties.API.Controllers.BaseController;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Report;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RBBH.ConnectedParties.Helpers.Excel;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

/// <summary>
/// API za upravljanje globalnim (jedinim) zapisom kapitala — modul Kapital.
/// </summary>
[ApiController]
[Route("api/capital")]
[Authorize]
public class CapitalController(ICapitalService capitalService, IAuditService auditService) : BaseResuItController
{
    private readonly ICapitalService _capitalService = capitalService;
    private readonly IAuditService _auditService = auditService;

    /// <summary>Vraća globalni zapis kapitala, ili null ako još nije postavljen.</summary>
    [HttpGet]
    [Authorize(Policy = ApplicationPolicies.CapitalRead)]
    [ProducesResponseType(typeof(CapitalDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<CapitalDTO?>> Get()
    {
        var result = await _capitalService.Get();
        return HandleResult(result);
    }

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

    /// <summary>Kreira ili ažurira globalni zapis kapitala (Regulatorni/Osnovni/Dopunski + datum).</summary>
    [HttpPut]
    [Authorize(Policy = ApplicationPolicies.CapitalEdit)]
    [ProducesResponseType(typeof(CapitalDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CapitalDTO>> Upsert([FromBody] UpdateCapitalDTO dto)
    {
        var korisnik = GetKorisnik();
        var result = await _capitalService.Upsert(dto, korisnik);

        if (result.IsSuccessful)
        {
            await _auditService.LogAsync(new AuditEntry
            {
                TableName = "Capital",
                RecordId = result.Value.Id.ToString(),
                Action = "UPSERT",
                NewValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    regulatoryCapital = result.Value.RegulatoryCapital,
                    coreCapital = result.Value.CoreCapital,
                    supplementaryCapital = result.Value.SupplementaryCapital,
                    capitalDate = result.Value.CapitalDate
                }),
                UserId = korisnik,
                Username = korisnik,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
        }

        return HandleResult(result);
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
