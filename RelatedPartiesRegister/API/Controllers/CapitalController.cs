using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.DTO.Capital;
using RBBH.ConnectedParties.DL.Entities.Capital;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;
using RBBH.ConnectedParties.Helpers.Excel;

namespace RBBH.ConnectedParties.API.Controllers;

[ApiController, Route("api/capital"), Authorize]
public sealed class CapitalController(ConnectedPartiesDbContext db) : ControllerBase
{
    [HttpGet, Authorize(Policy = ApplicationPolicies.CapitalRead)]
    public async Task<IReadOnlyList<CapitalResponseDTO>> GetAll() => await db.Capitals.AsNoTracking()
        .OrderByDescending(x => x.DatumKapitala).ThenByDescending(x => x.Id).Select(x => Map(x)).ToListAsync();

    [HttpPost, Authorize(Policy = ApplicationPolicies.CapitalCreate)]
    public async Task<ActionResult<CapitalResponseDTO>> Create(SaveCapitalDTO dto)
    {
        var error = Validate(dto); if (error is not null) return BadRequest(new ValidationProblemDetails(error));
        var date = dto.DatumKapitala!.Value.Date;
        if (await db.Capitals.AnyAsync(x => x.DatumKapitala == date))
            return Conflict(new ProblemDetails { Title = "Kapital za odabrani datum već postoji.", Detail = "Uredite postojeći zapis umjesto kreiranja duplikata." });
        var item = new Capital { OsnovniKapital = dto.OsnovniKapital!.Value, RegulatorniKapital = dto.RegulatorniKapital!.Value, DopunskiKapital = dto.DopunskiKapital!.Value, DatumKapitala = dto.DatumKapitala.Value.Date, CreatedBy = CurrentUser() };
        db.Capitals.Add(item); await db.SaveChangesAsync(); return Created($"/api/capital/{item.Id}", Map(item));
    }

    [HttpPut("{id:int}"), Authorize(Policy = ApplicationPolicies.CapitalEdit)]
    public async Task<ActionResult<CapitalResponseDTO>> Update(int id, SaveCapitalDTO dto)
    {
        var error = Validate(dto); if (error is not null) return BadRequest(new ValidationProblemDetails(error));
        var item = await db.Capitals.FindAsync(id); if (item is null) return NotFound();
        var date = dto.DatumKapitala!.Value.Date;
        if (await db.Capitals.AnyAsync(x => x.Id != id && x.DatumKapitala == date)) return Conflict(new ProblemDetails { Title = "Kapital za odabrani datum već postoji." });
        item.OsnovniKapital = dto.OsnovniKapital!.Value; item.RegulatorniKapital = dto.RegulatorniKapital!.Value; item.DopunskiKapital = dto.DopunskiKapital!.Value; item.DatumKapitala = dto.DatumKapitala.Value.Date; item.ModifiedAt = DateTime.UtcNow; item.ModifiedBy = CurrentUser();
        await db.SaveChangesAsync(); return Map(item);
    }

    [HttpDelete("{id:int}"), Authorize(Policy = ApplicationPolicies.CapitalDelete)]
    public async Task<IActionResult> Delete(int id) { var item = await db.Capitals.FindAsync(id); if (item is null) return NotFound(); db.Capitals.Remove(item); await db.SaveChangesAsync(); return NoContent(); }

    [HttpGet("export"), Authorize(Policy = ApplicationPolicies.CapitalRead)]
    public async Task<IActionResult> Export()
    {
        var items = await db.Capitals.AsNoTracking().OrderByDescending(x => x.DatumKapitala).ToListAsync();
        var bytes = RegistryExcelExporter.CreateReport("Kapital", ["Redni broj", "Osnovni kapital", "Regulatorni kapital", "Dopunski kapital", "Datum kapitala"], items.Select((x, i) => (IReadOnlyList<object?>)[i + 1, x.OsnovniKapital, x.RegulatorniKapital, x.DopunskiKapital, x.DatumKapitala]));
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"kapital-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx");
    }

    private static Dictionary<string, string[]>? Validate(SaveCapitalDTO dto)
    {
        var errors = new Dictionary<string, string[]>();
        if (dto.OsnovniKapital is null or < 0) errors["osnovniKapital"] = ["Osnovni kapital je obavezan i ne može biti negativan."];
        if (dto.RegulatorniKapital is null or < 0) errors["regulatorniKapital"] = ["Regulatorni kapital je obavezan i ne može biti negativan."];
        if (dto.DopunskiKapital is null or < 0) errors["dopunskiKapital"] = ["Dopunski kapital je obavezan i ne može biti negativan."];
        if (dto.DatumKapitala is null) errors["datumKapitala"] = ["Datum kapitala je obavezan."];
        return errors.Count == 0 ? null : errors;
    }
    private string CurrentUser() => HttpContext?.User.FindFirst("preferred_username")?.Value ?? HttpContext?.User.Identity?.Name ?? Environment.MachineName;
    private static CapitalResponseDTO Map(Capital x) => new() { Id = x.Id, OsnovniKapital = x.OsnovniKapital, RegulatorniKapital = x.RegulatorniKapital, DopunskiKapital = x.DopunskiKapital, DatumKapitala = x.DatumKapitala, CreatedAt = x.CreatedAt, CreatedBy = x.CreatedBy, ModifiedAt = x.ModifiedAt, ModifiedBy = x.ModifiedBy };
}
