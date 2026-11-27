using ClosedXML.Excel;
using RBBH.ConnectedParties.BL.Services;
using Microsoft.Data.SqlClient;
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
public partial class CodeListController(ICodeListService codeListService, ConnectedPartiesDbContext db, IAuditService auditService) : BaseResuItController
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
    public async Task<ActionResult<object>> CreateCategory([FromBody] CreateCodeListDefinitionDTO dto, CancellationToken ct)
    {
        var name = CodebookName.Canonical(dto.Name ?? "");
        if (name.Length < 2 || name.Length > 100 || dto.Description?.Length > 500)
            return BadRequest(new ProblemDetails { Title = BackendMessages.Message("errors.codelist.name.invalid.title"), Detail = BackendMessages.Message("errors.codelist.name.length") });
        if (dto.Columns is { } columns && !CodebookDefinitions.ValidColumns(columns))
            return BadRequest(new { code = "codes.schema_invalid" });
        ConflictObjectResult Duplicate() => Conflict(new ProblemDetails
        {
            Title = BackendMessages.Message("errors.codelist.duplicate.title"),
            Detail = BackendMessages.Message("errors.codelist.duplicate.detail", name)
        });
        if (await db.CodeListDefinitions.AnyAsync(item => item.Name.ToUpper() == name, ct) ||
            await db.CodeLists.IgnoreQueryFilters().AnyAsync(item => item.Definition!.Name.ToUpper() == name, ct))
            return Duplicate();
        var korisnik = GetKorisnik();
        var definition = new CodeListDefinition { Name = name, Description = dto.Description?.Trim(), CreatedBy = korisnik };
        db.CodeListDefinitions.Add(definition);
        if (dto.Columns is not null) CodebookDefinitions.Apply(db, definition, dto.Columns);
        else CodebookDefinitions.ConfigureReferenceFields(db, definition);
        db.AuditLogs.Add(Helpers.BusinessAudit.Create("CodeList", "CODELIST_ADD", korisnik, null,
            newValues: new { definition.Name, definition.Description, definition.IsActive, Columns = dto.Columns }));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException exception) when (exception.GetBaseException() is SqlException { Number: 2601 or 2627 })
        {
            return Duplicate();
        }
        return CreatedAtAction(nameof(Columns), new { name = definition.Name },
            new { definition.Id, definition.Name, definition.Description });
    }

    [HttpGet("categories/definitions")]
    public async Task<IActionResult> Definitions(CancellationToken ct, int page = 1, int? pageSize = null, string? search = null)
    {
        var query = db.CodeListDefinitions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(row => row.Name.ToUpper().Contains(term));
        }

        page = Math.Clamp(page, 1, 1_000_000);
        var size = Math.Clamp(pageSize ?? 30, 1, 100);
        var ordered = query.OrderBy(row => row.Name).ThenBy(row => row.Id);
        var rows = await (pageSize.HasValue ? ordered.Skip((page - 1) * size).Take(size) : ordered)
            .Select(row => new { row.Id, row.Name, row.Description, row.IsActive, row.IsSystemRequired,
                row.CreatedAt, row.CreatedBy, row.ModifiedAt, row.ModifiedBy }).ToListAsync(ct);
        var total = !pageSize.HasValue || (page == 1 && rows.Count < size) ? rows.Count : await query.CountAsync(ct);
        return Ok(new { items = rows, total, page, pageSize = size });
    }

    [HttpPut("categories/{name}/status"), Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    public async Task<IActionResult> SetDefinitionStatus(string name, CodebookStatusRequest input, CancellationToken ct)
    {
        if (input.IsActive is not { } isActive) return BadRequest();
        var definition = await db.CodeListDefinitions.SingleOrDefaultAsync(row => row.Name == name, ct);
        if (definition is null) return NotFound();
        if (definition.ModifiedAt != input.ExpectedModifiedAt) return Conflict(new { code = "codes.concurrent" });
        if (definition.IsActive == isActive) return NoContent();
        if (!isActive && definition.IsSystemRequired) return Conflict(new { code = "codes.status_protected" });

        var before = new { definition.Name, definition.IsActive };
        var actor = GetKorisnik();
        definition.IsActive = isActive;
        definition.ModifiedBy = actor;
        db.AuditLogs.Add(Helpers.BusinessAudit.Create("CodeList", "CODELIST_UPDATE", actor, null,
            before, new { definition.Name, definition.IsActive }));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { code = "codes.concurrent" }); }
        return NoContent();
    }

    [HttpGet("categories/{name}/columns")]
    public async Task<IActionResult> Columns(string name, CancellationToken ct)
    {
        var definition = await db.CodeListDefinitions.SingleOrDefaultAsync(row => row.Name == name, ct);
        if (definition is null) return NotFound();
        return Ok(new { items = CodebookCsv.Columns(db, definition) });
    }

    [HttpGet("{category}/csv-template")]
    public async Task<IActionResult> CsvTemplate(string category, CancellationToken ct)
    {
        var csv = new CodebookCsv(db);
        var definition = await csv.Find(category, ct);
        if (definition is null) return NotFound();
        var bytes = await csv.Template(definition, ct);
        if (bytes is null) return Conflict();
        return File(bytes, "text/csv; charset=utf-8", "codebook-template.csv");
    }

    [HttpPost("{category}/csv-import")]
    [Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(CodebookCsv.MaximumBytes + 64 * 1024)]
    public async Task<IActionResult> ImportCsv(string category, IFormFile? file, CancellationToken ct, [FromQuery] bool dryRun = true, [FromQuery] bool allowPartial = false)
    {
        if (file is null || file.Length is <= 0 or > CodebookCsv.MaximumBytes ||
            !Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            return Ok(new CsvPreview(0, 0, 0, 0, false, [new("codes.csv.file")]));
        var csv = new CodebookCsv(db);
        var definition = await csv.Find(category, ct);
        if (definition is null) return NotFound();

        await using var stream = file.OpenReadStream();
        try { return Ok(await csv.Import(definition, stream, dryRun, GetKorisnik(), ct, allowPartial)); }
        catch (DbUpdateException exception) when (exception is DbUpdateConcurrencyException ||
            exception.GetBaseException() is SqlException { Number: 2601 or 2627 })
        {
            return Conflict(new ProblemDetails { Status = 409, Title = BackendMessages.Message("errors.codelist.concurrent.change") });
        }
    }

    [HttpPut("categories/{name}"), Authorize(Policy = ApplicationPolicies.AdministrationWrite)]
    public async Task<IActionResult> UpdateDefinition(string name, UpdateCodeListDefinitionDTO input, CancellationToken ct)
    {
        if (input.Description?.Length > 500) return BadRequest(new ProblemDetails { Status = 400, Title = BackendMessages.Message("errors.codelist.description.length") });
        var definition = await db.CodeListDefinitions.SingleOrDefaultAsync(row => row.Name == name, ct);
        if (definition is null) return NotFound();
        if (definition.ModifiedAt != input.ExpectedModifiedAt) return Conflict(new ProblemDetails { Status = 409, Title = BackendMessages.Message("errors.codelist.concurrent.change") });
        var previousColumns = !CodebookDefinitions.UsesReferenceFields(definition) ? CodebookCsv.Columns(db, definition) : null;
        if (input.Columns is { } columns)
        {
            if (!CodebookDefinitions.ValidColumns(columns)) return BadRequest(new { code = "codes.schema_invalid" });
            if (CodebookDefinitions.UsesReferenceFields(definition) || !definition.IsActive ||
                (previousColumns!.Any(column => !columns.Any(next => next.Slot == column.Slot)) &&
                 await db.CodeLists.IgnoreQueryFilters().AnyAsync(row => row.Definition!.Name == definition.Name, ct)))
                return Conflict(new { code = "codes.schema_protected" });
            CodebookDefinitions.Apply(db, definition, columns);
        }
        var before = new { definition.Description, Columns = previousColumns };
        var actor = GetKorisnik();
        definition.Description = input.Description?.Trim();
        definition.ModifiedBy = actor;
        db.AuditLogs.Add(Helpers.BusinessAudit.Create("CodeList", "CODELIST_UPDATE", actor, null,
            before, new { definition.Description, Columns = previousColumns is null ? null : CodebookCsv.Columns(db, definition) }));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict(new { code = "codes.concurrent" }); }
        return Ok(new { definition.Id, definition.Name, definition.Description, definition.ModifiedAt, definition.ModifiedBy });
    }

    /// <summary>
    /// Briše cijelu definiciju šifrarnika i sve njene vrijednosti trajno.
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
    public async Task<ActionResult<object>> GetAll([FromRoute] string kategorija, int page = 1, int? pageSize = null)
    {
        if (string.IsNullOrWhiteSpace(kategorija))
            return BadRequest(BackendMessages.Message("validation.codelist.category.required"));

        var ct = HttpContext.RequestAborted;
        var definition = await db.CodeListDefinitions.SingleOrDefaultAsync(row => row.Name == kategorija, ct);
        var columns = definition is null ? [] : CodebookCsv.Columns(db, definition).ToArray();
        var definitionId = definition?.Id;
        var query = db.CodeLists.IgnoreQueryFilters().Where(row => row.DefinitionId == definitionId);
        var ordered = query.OrderBy(row => row.Naziv).ThenBy(row => row.ID);
        page = Math.Clamp(page, 1, 1_000_000);
        var size = Math.Clamp(pageSize ?? 50, 1, 100);
        var rows = await (pageSize.HasValue ? ordered.Skip((page - 1) * size).Take(size) : ordered).ToListAsync(ct);
        var total = !pageSize.HasValue || (page == 1 && rows.Count < size) ? rows.Count : await query.CountAsync(ct);

        var items = rows.Select(row => new
        {
            id = row.ID,
            category = definition!.Name,
            code = row.Kod,
            value = row.Naziv,
            description = row.Opis,
            isActive = row.Aktivan,
            localization = row.Localization,
            slotValues = columns.Length == 0 ? null : columns.ToDictionary(column => column.Slot, column => CodebookCsv.ReadValue(db, row, column.Slot)),
            modifiedBy = row.IzmijenioKorisnik ?? row.KreiraoKorisnik,
            modifiedAt = row.IzmijenjenDatum ?? row.KreiranDatum
        }).ToArray();

        var metadata = definition is null ? null : new
        {
            definition.Id, definition.Name, definition.Description, definition.IsActive, definition.IsSystemRequired,
            definition.CreatedAt, definition.CreatedBy, definition.ModifiedAt, definition.ModifiedBy
        };
        return Ok(new { category = kategorija, items, columns, definition?.ModifiedAt, total, page, pageSize = size, definition = metadata });
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
        try
        {
            var result = await _codeListService.Create(dto, korisnik);
            if (result.IsSuccessful)
                return CreatedAtAction(nameof(GetByID), new { id = result.Value.ID }, result.Value);
            return HTTPExceptiontFromResult(result);
        }
        catch (DbUpdateException exception) when (exception is DbUpdateConcurrencyException ||
            exception.GetBaseException() is SqlException { Number: 2601 or 2627 })
        {
            return Conflict(new { code = "codes.concurrent" });
        }
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
            Naziv = request.Value.Trim()
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
        var values = await db.CodeLists.AsNoTracking().Where(row => row.Definition!.Name == kategorija)
            .OrderBy(row => row.Naziv).ThenBy(row => row.ID)
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
        if (definition is not null && !CodebookDefinitions.UsesReferenceFields(definition)) return BadRequest(new { code = "codes.schema_invalid" });
        if (definition is { IsActive: false }) return BadRequest(new { error = BackendMessages.Message("validation.codelist.inactive") });
        if (definition is null)
        {
            definition = new CodeListDefinition { Name = kategorija.Trim(), CreatedBy = korisnik };
            db.CodeListDefinitions.Add(definition);
        }
        var usedCodes = (await db.CodeLists.IgnoreQueryFilters().Where(row => row.Definition!.Name.ToUpper() == normalizedCategory)
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
            db.CodeLists.Add(new() { Kategorija = definition.Name, Kod = code, Naziv = val,
                KreiraoKorisnik = korisnik, KreiranDatum = DateTime.UtcNow });
            preview.Imported++;
        }
        definition.ModifiedBy = korisnik;
        db.AuditLogs.Add(new() { TableName = "CodeList", Action = "IMPORT",
            Username = korisnik, Timestamp = DateTime.UtcNow,
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
