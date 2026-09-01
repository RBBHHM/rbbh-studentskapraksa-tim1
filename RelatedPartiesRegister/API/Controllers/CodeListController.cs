using ClosedXML.Excel;
using RBBH.ConnectedParties.API.Controllers.BaseController;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Sifarnici;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using RBBH.ConnectedParties.DL.Persistence;

namespace RBBH.ConnectedParties.API.Controllers;

/// <summary>
/// PL-36: CRUD API za upravljanje šifarnicima.
/// Izmjene se odmah reflektuju u svim padajućim menijima.
/// Sve promjene bilježe korisnika i datum izmjene (PL-33).
/// </summary>
[ApiController]
[Route("api/code-lists")]
[Authorize(Policy = "application-administration")]
public class CodeListController(ICodeListService codeListService, ConnectedPartiesDbContext db) : BaseResuItController
{
    private readonly ICodeListService _codeListService = codeListService;

    // ─── Lista kategorija ─────────────────────────────────────────────────────

    /// <summary>
    /// Vraća listu svih kategorija šifarnika.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetCategories()
    {
        var result = await _codeListService.GetCategoriesAsync();
        return HandleResult(result);
    }

    [HttpPost("categories")]
    public async Task<ActionResult<object>> CreateCategory([FromBody] CreateCodeListDefinitionDTO dto)
    {
        var name = dto.Name.Trim();
        if (name.Length < 2 || name.Length > 100)
            return BadRequest(new ProblemDetails { Title = "Naziv nije ispravan", Detail = "Naziv definicije mora imati između 2 i 100 znakova." });
        if (await db.CodeListDefinitions.AnyAsync(item => item.Name == name) || await db.CodeLists.IgnoreQueryFilters().AnyAsync(item => item.Kategorija == name))
            return Conflict(new ProblemDetails { Title = "Definicija već postoji", Detail = $"Šifrarnik '{name}' već postoji." });
        var definition = new CodeListDefinition { Name = name, Description = dto.Description?.Trim(), CreatedBy = GetKorisnik() };
        db.CodeListDefinitions.Add(definition);
        await db.SaveChangesAsync();
        return Created($"{Request.Path}/{definition.Id}", new { definition.Id, definition.Name, definition.Description });
    }

    /// <summary>
    /// Briše cijelu definiciju šifrarnika i sve njene vrijednosti metodom soft-delete.
    /// </summary>
    [HttpDelete("categories/{name}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> DeleteCategory([FromRoute] string name)
    {
        var result = await _codeListService.DeleteCategory(name, GetKorisnik());
        if (!result.IsSuccessful) return HandleResult(result);

        return Ok(new { message = $"Šifrarnik '{name}' je uspješno obrisan." });
    }

    // ─── Administracija ───────────────────────────────────────────────────────

    /// <summary>
    /// Vraća sve vrijednosti (uključujući neaktivne) za datu kategoriju — za administratorski pregled.
    /// </summary>
    [HttpGet("{kategorija}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> GetAll([FromRoute] string kategorija)
    {
        var result = await _codeListService.GetAllByKategorija(kategorija);
        if (!result.IsSuccessful) return HandleResult(result);

        // Map to API contract format (English field names)
        var items = result.Value.Select(x => new
        {
            id          = x.ID,
            category    = x.Kategorija,
            code        = x.Kod,
            value       = x.Naziv,
            description = x.Opis,
            displayOrder = x.RedoslijedPrikaza,
            isActive    = x.Aktivan,
            modifiedBy  = x.IzmijenioKorisnik ?? x.KreiraoKorisnik,
            modifiedAt  = x.IzmijenjenDatum   ?? x.KreiranDatum
        }).ToList();

        return Ok(new { category = kategorija, items });
    }

    /// <summary>
    /// Vraća jednu vrijednost šifarnika po ID-u.
    /// </summary>
    [HttpGet("id/{id:int}")]
    [ProducesResponseType(typeof(CodeListResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CodeListResponseDTO>> GetByID([FromRoute] int id)
    {
        var result = await _codeListService.GetByID(id);
        return HandleResult(result);
    }

    /// <summary>
    /// Kreira novu vrijednost šifarnika (puni DTO format).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CodeListResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CodeListResponseDTO>> Create([FromBody] CreateCodeListDTO dto)
    {
        var korisnik = GetKorisnik();
        var result   = await _codeListService.Create(dto, korisnik);

        if (result.IsSuccessful)
            return Created($"{Request.Path}/{result.Value.ID}", result.Value);

        return HTTPExceptiontFromResult(result);
    }

    /// <summary>
    /// API contract: POST /api/code-lists/{category} { "value": "Nova vrijednost" }
    /// </summary>
    [HttpPost("{kategorija}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CreateInCategory(
        [FromRoute] string kategorija,
        [FromBody] SimpleValueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Value))
            return BadRequest(new { errors = new[] { new { field = "value", message = "Vrijednost je obavezna." } } });

        var korisnik = GetKorisnik();
        var kod = request.Value.Trim()
            .ToUpperInvariant()
            .Replace(" ", "_")
            .Replace("Š", "S").Replace("Č", "C").Replace("Ć", "C")
            .Replace("Ž", "Z").Replace("Đ", "DJ");

        var dto = new CreateCodeListDTO
        {
            Kategorija        = kategorija,
            Kod               = kod[..Math.Min(kod.Length, 50)],
            Naziv             = request.Value.Trim(),
            RedoslijedPrikaza = null
        };

        var result = await _codeListService.Create(dto, korisnik);

        if (result.IsSuccessful)
        {
            var item = result.Value;
            return Created($"{Request.Path}/{item.ID}", new
            {
                id         = item.ID,
                value      = item.Naziv,
                isActive   = item.Aktivan,
                modifiedBy = item.KreiraoKorisnik,
                modifiedAt = item.KreiranDatum
            });
        }

        return HTTPExceptiontFromResult(result);
    }

    /// <summary>
    /// Ažurira naziv, opis i redoslijed prikaza šifarnika.
    /// Automatski bilježi korisnika i datum izmjene.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> Update(
        [FromRoute] int id,
        [FromBody] UpdateCodeListDTO dto)
    {
        var korisnik = GetKorisnik();
        var result   = await _codeListService.Update(id, dto, korisnik);
        if (!result.IsSuccessful) return HandleResult(result);

        var x = result.Value;
        return Ok(new
        {
            id         = x.ID,
            value      = x.Naziv,
            isActive   = x.Aktivan,
            modifiedBy = x.IzmijenioKorisnik,
            modifiedAt = x.IzmijenjenDatum
        });
    }

    /// <summary>
    /// PL-37: Briše vrijednost šifarnika (soft-delete).
    /// Ako je vrijednost u upotrebi, vraća HTTP 400 s upozorenjem — ne briše.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> Delete([FromRoute] int id)
    {
        var korisnik = GetKorisnik();
        var result = await _codeListService.Delete(id, korisnik);

        if (result.IsSuccessful)
            return Ok(new { message = "Šifarnik je uspješno obrisan." });

        return HTTPExceptiontFromResult(result);
    }

    // ─── Excel import ────────────────────────────────────────────────────────

    /// <summary>
    /// Uvozi vrijednosti iz Excel fajla (.xlsx) u datu kategoriju šifarnika.
    /// dryRun=true → vraća preview bez pisanja u bazu.
    /// dryRun=false → uvozi nove vrijednosti i vraća rezultate.
    /// Excel format: prva kolona (A) sadrži vrijednosti. Zaglavlje se automatski detektuje.
    /// </summary>
    [HttpPost("{kategorija}/import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportPreviewDto>> ImportFromExcel(
        [FromRoute] string kategorija,
        IFormFile file,
        [FromQuery] bool dryRun = true)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Fajl je obavezan." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
            return BadRequest(new { error = "Prihvataju se samo .xlsx fajlovi." });
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { error = "Excel datoteka smije imati najviše 10 MB." });

        var rawValues = new List<(int Row, string Value)>();
        var validationErrors = new List<string>();

        // ClosedXML/OpenXML requires a seekable stream — copy IFormFile into MemoryStream first.
        using var seekable = new MemoryStream();
        await file.CopyToAsync(seekable);
        seekable.Position = 0;

        try
        {
            using var workbook = new XLWorkbook(seekable);
            var ws = workbook.Worksheet(1);
            var lastColumn = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            if (lastColumn != 1)
                return BadRequest(new { error = $"Predložak šifarnika mora sadržavati tačno jednu kolonu, a pronađeno je {lastColumn}." });
            if (lastRow < 2)
                return BadRequest(new { error = "Excel datoteka mora sadržavati zaglavlje i najmanje jedan red podataka." });
            if (lastRow - 1 > 5_000)
                return BadRequest(new { error = "Dozvoljeno je najviše 5.000 redova po uvozu." });

            var header = ws.Cell(1, 1).GetString().Trim().ToLowerInvariant();
            if (header is not ("vrijednost" or "value" or "naziv" or "name"))
                return BadRequest(new { error = $"Neispravno zaglavlje kolone A. Očekivano je 'Vrijednost' ili 'Value', a pronađeno '{ws.Cell(1, 1).GetString().Trim()}'." });

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                var val = row.Cell(1).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(val)) continue;

                if (val.Length > 200)
                {
                    validationErrors.Add($"Red {row.RowNumber()}: naziv smije imati najviše 200 znakova.");
                    continue;
                }
                rawValues.Add((row.RowNumber(), val));
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return BadRequest(new { error = "Excel datoteka nije ispravna ili je oštećena. Sačuvajte je kao .xlsx i pokušajte ponovo." });
        }

        var existingResult = await _codeListService.GetAllByKategorija(kategorija);
        var existing = existingResult.IsSuccessful
            ? existingResult.Value.Select(x => x.Naziv).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in rawValues.GroupBy(item => item.Value, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
            validationErrors.Add($"Redovi {string.Join(", ", group.Select(item => item.Row))}: vrijednost '{group.Key}' je ponovljena u datoteci.");

        var unique    = rawValues.Select(item => item.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var toImport  = unique.Where(v => !existing.Contains(v)).ToList();
        var duplicates = unique.Where(v =>  existing.Contains(v)).ToList();

        var preview = new ImportPreviewDto
        {
            TotalRows  = rawValues.Count,
            ToImport   = toImport,
            Duplicates = duplicates,
            Errors = validationErrors
        };

        if (dryRun)
            return Ok(preview);

        var korisnik = GetKorisnik();
        foreach (var val in toImport)
        {
            var kod = val.ToUpperInvariant()
                .Replace(" ", "_")
                .Replace("Š", "S").Replace("Č", "C").Replace("Ć", "C")
                .Replace("Ž", "Z").Replace("Đ", "DJ");

            var result = await _codeListService.Create(new CreateCodeListDTO
            {
                Kategorija = kategorija,
                Kod        = kod[..Math.Min(kod.Length, 50)],
                Naziv      = val
            }, korisnik);

            if (result.IsSuccessful) preview.Imported++;
        }

        return Ok(preview);
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Čita korisničko ime iz JWT claimova, ili fallback na host name.
    /// Prilagoditi prema konkretnoj autentifikaciji projekta.
    /// </summary>
    private string GetKorisnik()
    {
        return User.Identity?.Name
            ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
            ?? User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
            ?? Environment.MachineName;
    }
}
