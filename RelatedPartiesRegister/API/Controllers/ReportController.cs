// File: src/RBBH.ConnectedParties/API/Controllers/ReportController.cs

using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace RBBH.ConnectedParties.API.Controllers;

/// <summary>
/// Endpointi za pregled izvještaja i Excel export podataka o pravnim licima.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize(Roles = "regulatory-reporting")]
[Produces("application/json")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    private string CurrentUser() =>
        User.FindFirst(ClaimTypes.Name)?.Value
        ?? User.FindFirst("preferred_username")?.Value
        ?? "system";

    // ── Dnevni izvještaji ─────────────────────────────────────────────────────

    /// <summary>Kreira dnevni izvještaj za trenutni datum.</summary>
    [HttpPost("daily")]
    [ProducesResponseType(typeof(ReportDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateDailyReport()
    {
        var report = await _reportService.GenerateDailyReportAsync(CurrentUser());
        return CreatedAtAction(nameof(GetDailyReports), report);
    }

    /// <summary>
    /// Vraća paginiranu listu dnevnih izvještaja, sortirano od najnovijeg.
    /// </summary>
    [HttpGet("daily")]
    [ProducesResponseType(typeof(ReportListDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDailyReports(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _reportService.GetDailyReportsAsync(page, pageSize);
        return Ok(result);
    }

    // ── Mjesečni izvještaji ───────────────────────────────────────────────────

    /// <summary>
    /// Kreira mjesečni izvještaj za zadani mjesec.
    /// </summary>
    /// <param name="year">Godina (npr. 2026).</param>
    /// <param name="month">Mjesec (1-12).</param>
    [HttpPost("monthly/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(ReportDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateMonthlyReport(int year, int month)
    {
        if (month < 1 || month > 12)
            return BadRequest(new { errors = new[] { new { field = "month", message = "Mjesec mora biti između 1 i 12." } } });

        if (year < 2000 || year > DateTime.UtcNow.Year)
            return BadRequest(new { errors = new[] { new { field = "year", message = "Godina nije ispravna." } } });

        var report = await _reportService.GenerateMonthlyReportAsync(year, month, CurrentUser());
        return CreatedAtAction(nameof(GetMonthlyReports), report);
    }

    /// <summary>Vraća paginiranu listu mjesečnih izvještaja, sortirano od najnovijeg.</summary>
    [HttpGet("monthly")]
    [ProducesResponseType(typeof(ReportListDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMonthlyReports(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _reportService.GetMonthlyReportsAsync(page, pageSize);
        return Ok(result);
    }

    // ── Excel Export ──────────────────────────────────────────────────────────

    /// <summary>
    /// Excel export jednog klijenta po poreznom broju ili FBA ID-u.
    /// Vraća .xlsx fajl za download.
    /// </summary>
    /// <param name="identifier">Porezni broj (13 cifara) ili FBA ID (max 10 cifara).</param>
    [HttpGet("export/client/{identifier}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportClient(string identifier)
    {
        var bytes = await _reportService.ExportClientByIdAsync(identifier);
        var fileName = $"klijent_{identifier}_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    /// <summary>
    /// Excel export svih klijenata s definisanim limitima.
    /// Vraća .xlsx fajl za download.
    /// </summary>
    [HttpGet("export/all-clients")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportAllClients()
    {
        var bytes = await _reportService.ExportAllClientsWithLimitsAsync();
        var fileName = $"svi_klijenti_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet("export/generated/{reportId:guid}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportGenerated(Guid reportId)
    {
        var bytes = await _reportService.ExportGeneratedReportAsync(reportId);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"izvjestaj_{reportId:N}.xlsx");
    }
}
