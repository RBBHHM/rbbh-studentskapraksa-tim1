using ClosedXML.Excel;
using RBBH.ConnectedParties.API.Controllers.BaseController;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Sifarnici;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

/// <summary>
/// PL-36: CRUD API za upravljanje šifarnicima.
/// Izmjene se odmah reflektuju u svim padajućim menijima.
/// Sve promjene bilježe korisnika i datum izmjene (PL-33).
/// </summary>
[RBBH.ConnectedParties.Helpers.RequireActorEmail]
[ApiController]
[Route("api/code-lists")]
[Authorize(Policy = ApplicationPolicies.CodeListsRead)]
public class CodeListController(ICodeListService codeListService, ConnectedPartiesDbContext db, IAuditService auditService) : BaseResuItController
{
    private readonly ICodeListService _codeListService = codeListService;
    // Kept in the constructor for source compatibility; mutations now persist their audit in the same SaveChanges.
    private readonly IAuditService _auditService = auditService;

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
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    public async Task<ActionResult<object>> CreateCategory([FromBody] CreateCodeListDefinitionDTO dto)
    {
        var name = dto.Name?.Trim() ?? string.Empty;
        if (name.Length < 2 || name.Length > 100 || dto.Description?.Length > 500)
            return BadRequest(new ProblemDetails { Title = BackendMessages.Message("errors.codelist.name.invalid.title"), Detail = BackendMessages.Message("errors.codelist.name.length") });
        if (await db.CodeListDefinitions.AnyAsync(item => item.Name.Trim().ToUpper() == name.ToUpper()) || await db.CodeLists.IgnoreQueryFilters().AnyAsync(item => item.Kategorija.Trim().ToUpper() == name.ToUpper()))
            return Conflict(new ProblemDetails { Title = BackendMessages.Message("errors.codelist.duplicate.title"), Detail = BackendMessages.Message("errors.codelist.duplicate.detail", name) });
        var korisnik = GetKorisnik();
        var definition = new CodeListDefinition { Name = name, Description = dto.Description?.Trim(), CreatedBy = korisnik };
        db.CodeListDefinitions.Add(definition);
        db.AuditLogs.Add(new() { TableName = "CodeList", RecordId = definition.Id.ToString(), Action = "CODELIST_ADD",
            UserId = korisnik, Username = korisnik, Timestamp = DateTime.UtcNow });
        await db.SaveChangesAsync(HttpContext.RequestAborted);



        return Created($"{Request.Path}/{definition.Id}", new { definition.Id, definition.Name, definition.Description });
    }

    [HttpGet("categories/definitions")]
    public async Task<IActionResult> Definitions(CancellationToken ct) => Ok(new
    {
        items = await db.CodeListDefinitions.AsNoTracking().OrderBy(row => row.Name).Select(row => new
        { row.Id, row.Name, row.Description, row.IsActive, row.IsSystemRequired, row.Version, row.CreatedAt, row.CreatedBy, row.ModifiedAt, row.ModifiedBy }).ToListAsync(ct)
    });

    [HttpPut("categories/{name}"), Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    public async Task<IActionResult> UpdateDefinition(string name, UpdateCodeListDefinitionDTO input, CancellationToken ct)
    {
        if (input.Description?.Length > 500) return BadRequest(new ProblemDetails { Status = 400, Title = BackendMessages.Message("errors.codelist.description.length") });
        var definition = await db.CodeListDefinitions.SingleOrDefaultAsync(row => row.Name == name, ct);
        if (definition is null) return NotFound();
        if (definition.Version != input.ExpectedVersion) return Conflict(new ProblemDetails { Status = 409, Title = BackendMessages.Message("errors.codelist.concurrent.change") });
        var actor = GetKorisnik(); definition.Description = input.Description?.Trim(); definition.ModifiedBy = actor;
        db.AuditLogs.Add(new() { TableName = "CodeList", RecordId = definition.Id.ToString(), Action = "CODELIST_UPDATE",
            UserId = actor, Username = actor, Timestamp = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        return Ok(new { definition.Id, definition.Name, definition.Description, definition.Version, definition.ModifiedAt, definition.ModifiedBy });
    }

    /// <summary>
    /// Briše cijelu definiciju šifrarnika i sve njene vrijednosti metodom soft-delete.
    /// </summary>
    [HttpDelete("categories/{name}")]
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> DeleteCategory([FromRoute] string name)
    {
        var korisnik = GetKorisnik();
        var result = await _codeListService.DeleteCategory(name, korisnik);
        // Return the HTTP result, not ActionResult<bool> as an object/HTTP 200.
        if (!result.IsSuccessful) return HandleResult(result).Result!;

        return Ok(new { message = BackendMessages.Message("messages.codelist.definition.deleted", name) });
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
            id = x.ID,
            category = x.Kategorija,
            code = x.Kod,
            value = x.Naziv,
            description = x.Opis,
            displayOrder = x.RedoslijedPrikaza,
            isActive = x.Aktivan,
            modifiedBy = x.IzmijenioKorisnik ?? x.KreiraoKorisnik,
            modifiedAt = x.IzmijenjenDatum ?? x.KreiranDatum
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
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    [ProducesResponseType(typeof(CodeListResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CodeListResponseDTO>> Create([FromBody] CreateCodeListDTO dto)
    {
        var korisnik = GetKorisnik();
        var result = await _codeListService.Create(dto, korisnik);

        if (result.IsSuccessful)
        {
            return Created($"{Request.Path}/{result.Value.ID}", result.Value);
        }

        return HTTPExceptiontFromResult(result);
    }

    /// <summary>
    /// API contract: POST /api/code-lists/{category} { "value": "Nova vrijednost" }
    /// </summary>
    [HttpPost("{kategorija}")]
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> CreateInCategory(
        [FromRoute] string kategorija,
        [FromBody] SimpleValueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Value))
            return BadRequest(new { errors = new[] { new { field = "value", message = BackendMessages.Message("errors.codelist.value.required") } } });

        var korisnik = GetKorisnik();

        var dto = new CreateCodeListDTO
        {
            Kategorija = kategorija,
            Kod = ValueCode(request.Value.Trim()),
            Naziv = request.Value.Trim(),
            RedoslijedPrikaza = null
        };

        var result = await _codeListService.Create(dto, korisnik);

        if (result.IsSuccessful)
        {
            var item = result.Value;

            return Created($"{Request.Path}/{item.ID}", new
            {
                id = item.ID,
                value = item.Naziv,
                isActive = item.Aktivan,
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
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> Update(
        [FromRoute] int id,
        [FromBody] UpdateCodeListDTO dto)
    {
        var korisnik = GetKorisnik();
        var result = await _codeListService.Update(id, dto, korisnik);
        // Preserve validation/not-found status across different response DTOs.
        if (!result.IsSuccessful) return HandleResult(result).Result!;

        var x = result.Value;

        return Ok(new
        {
            id = x.ID,
            value = x.Naziv,
            isActive = x.Aktivan,
            modifiedBy = x.IzmijenioKorisnik,
            modifiedAt = x.IzmijenjenDatum
        });
    }

    /// <summary>
    /// PL-37: Briše vrijednost šifarnika (soft-delete).
    /// Ako je vrijednost u upotrebi, vraća HTTP 400 s upozorenjem — ne briše.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> Delete([FromRoute] int id)
    {
        var korisnik = GetKorisnik();
        var result = await _codeListService.Delete(id, korisnik);

        if (result.IsSuccessful)
        {
            return Ok(new { message = BackendMessages.Message("messages.codelist.deleted") });
        }

        return HTTPExceptiontFromResult(result);
    }

    /// <summary>Exports the label-only workbook accepted by the existing legacy import contract.</summary>
    [HttpGet("{kategorija}/export")]
    public async Task<IActionResult> Export(string kategorija, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(kategorija) || kategorija.Length > 100)
            return BadRequest(new ProblemDetails { Status = 400, Title = BackendMessages.Message("errors.codelist.name.invalid") });
        var values = await db.CodeLists.AsNoTracking().Where(row => row.Kategorija == kategorija)
            .OrderBy(row => row.RedoslijedPrikaza).ThenBy(row => row.Naziv).ThenBy(row => row.ID)
            .Select(row => row.Naziv).Take(5001).ToListAsync(ct);
        if (values.Count > 5000) return BadRequest(new ProblemDetails { Status = 400, Title = BackendMessages.Message("errors.workbook.export.limit") });
        var bytes = RBBH.ConnectedParties.Helpers.Excel.RegistryExcelExporter.Create("Šifrarnik", ["Vrijednost"],
            values.Select(value => (IReadOnlyList<object?>)[value]));
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "codebook.xlsx");
    }

    // ─── Excel import ────────────────────────────────────────────────────────

    /// <summary>
    /// Uvozi vrijednosti iz Excel fajla (.xlsx) u datu kategoriju šifarnika.
    /// dryRun=true → vraća preview bez pisanja u bazu.
    /// dryRun=false → uvozi nove vrijednosti i vraća rezultate.
    /// Excel format: prva kolona (A) sadrži vrijednosti. Zaglavlje se automatski detektuje.
    /// </summary>
    [HttpPost("{kategorija}/import")]
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportPreviewDto>> ImportFromExcel(
        [FromRoute] string kategorija,
        IFormFile file,
        [FromQuery] bool dryRun = true)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = BackendMessages.Message("workbook.codelist.file.required") });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
            return BadRequest(new { error = BackendMessages.Message("workbook.codelist.file.format") });
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { error = BackendMessages.Message("errors.workbook.size") });

        var rawValues = new List<(int Row, string Value)>();
        var validationErrors = new List<string>();

        // ClosedXML/OpenXML requires a seekable stream — copy IFormFile into MemoryStream first.
        using var seekable = new MemoryStream();
        await file.CopyToAsync(seekable, HttpContext.RequestAborted);
        seekable.Position = 0;

        try
        {
            RBBH.ConnectedParties.Helpers.Excel.WorkbookEnvelope.Validate(seekable);
            using var workbook = new XLWorkbook(seekable);
            var ws = workbook.Worksheet(1);
            var lastColumn = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;
            if (lastColumn != 1)
                return BadRequest(new { error = BackendMessages.Message("workbook.codelist.schema.count", lastColumn) });
            if (lastRow < 2)
                return BadRequest(new { error = BackendMessages.Message("workbook.codelist.data.required") });
            if (lastRow - 1 > 5_000)
                return BadRequest(new { error = BackendMessages.Message("workbook.codelist.data.limit") });

            if (ws.CellsUsed().Any(cell => cell.HasFormula))
                return BadRequest(new { error = BackendMessages.Message("workbook.data.formula") });
            var header = ws.Cell(1, 1).GetString().Trim().ToLowerInvariant();
            if (header is not ("vrijednost" or "value" or "naziv" or "name"))
                return BadRequest(new { error = BackendMessages.Message("workbook.codelist.header", ws.Cell(1, 1).GetString().Trim()) });

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                HttpContext.RequestAborted.ThrowIfCancellationRequested();
                var val = row.Cell(1).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(val)) continue;

                if (val.Length > 200)
                {
                    validationErrors.Add(BackendMessages.Message("workbook.codelist.name.length", row.RowNumber()));
                    continue;
                }
                rawValues.Add((row.RowNumber(), val));
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return BadRequest(new { error = BackendMessages.Message("workbook.invalid") });
        }

        var existingResult = await _codeListService.GetAllByKategorija(kategorija);
        var existing = existingResult.IsSuccessful
            ? existingResult.Value.Select(x => x.Naziv).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in rawValues.GroupBy(item => item.Value, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
            validationErrors.Add($"Redovi {string.Join(", ", group.Select(item => item.Row))}: vrijednost '{group.Key}' je ponovljena u datoteci.");

        var unique = rawValues.Select(item => item.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var toImport = unique.Where(v => !existing.Contains(v)).ToList();
        var duplicates = unique.Where(v => existing.Contains(v)).ToList();

        var preview = new ImportPreviewDto
        {
            TotalRows = rawValues.Count,
            ToImport = toImport,
            Duplicates = duplicates,
            Errors = validationErrors
        };

        if (dryRun)
            return Ok(preview);

        var korisnik = GetKorisnik();
        var normalizedCategory = kategorija.Trim().ToUpperInvariant();
        if (kategorija.Trim().Length is < 1 or > 100) return BadRequest(new { error = BackendMessages.Message("errors.codelist.name.invalid") });
        var definition = await db.CodeListDefinitions.SingleOrDefaultAsync(item => item.Name.ToUpper() == normalizedCategory, HttpContext.RequestAborted);
        if (definition is { IsActive: false }) return BadRequest(new { error = BackendMessages.Message("validation.codelist.inactive") });
        if (definition is null)
        {
            definition = new CodeListDefinition { Name = kategorija.Trim(), CreatedBy = korisnik };
            db.CodeListDefinitions.Add(definition);
        }
        var usedCodes = (await db.CodeLists.IgnoreQueryFilters().Where(row => row.Kategorija.ToUpper() == normalizedCategory)
            .Select(row => row.Kod).ToListAsync(HttpContext.RequestAborted)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var val in toImport)
        {
            HttpContext.RequestAborted.ThrowIfCancellationRequested();
            var code = ValueCode(val);
            if (!usedCodes.Add(code))
            {
                preview.Errors.Add(BackendMessages.Message("workbook.codelist.code.duplicate", val));
                continue;
            }
            db.CodeLists.Add(new() { Kategorija = definition.Name, DefinitionId = definition.Id, Kod = code, Naziv = val,
                KreiraoKorisnik = korisnik, KreiranDatum = DateTime.UtcNow });
            preview.Imported++;
        }
        definition.ModifiedBy = korisnik;
        db.AuditLogs.Add(new() { TableName = "CodeList", RecordId = definition.Name, Action = "IMPORT",
            UserId = korisnik, Username = korisnik, Timestamp = DateTime.UtcNow,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { imported = preview.Imported, failed = preview.Errors.Count }) });
        await db.SaveChangesAsync(HttpContext.RequestAborted);

        return Ok(preview);
    }

    private static string ValueCode(string value)
    {
        var code = value.ToUpperInvariant().Replace(" ", "_").Replace("Š", "S").Replace("Č", "C").Replace("Ć", "C").Replace("Ž", "Z").Replace("Đ", "DJ");
        return code.Length <= 50 ? code : code[..33] + "_" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value.ToUpperInvariant())))[..16];
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Uses the authenticated username or stable subject; no fabricated actor fallback.
    /// Prilagoditi prema konkretnoj autentifikaciji projekta.
    /// </summary>
    private string GetKorisnik()
    {
        return User.FindFirst("preferred_username")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? RBBH.ConnectedParties.Helpers.AuthenticatedActor.RequireSubject(User);
    }
}
