// File: src/RBBH.ConnectedParties/API/Controllers/LegalEntityController.cs

using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO;
using RBBH.ConnectedParties.DL.DTO.LegalEntity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using RBBH.ConnectedParties.Helpers.Excel;
using RBBH.ConnectedParties.Helpers.Constants;

namespace RBBH.ConnectedParties.API.Controllers;

/// <summary>
/// Controller za upravljanje registrom povezanih pravnih lica.
/// </summary>
[RBBH.ConnectedParties.Helpers.RequireActorEmail]
[ApiController]
[Route("api/legal-entities")]
[Authorize]
[Produces("application/json")]
public class LegalEntityController : ControllerBase
{
    private readonly ILegalEntityService _legalEntityService;
    private readonly IAuditService _auditService;

    public LegalEntityController(ILegalEntityService legalEntityService, IAuditService auditService)
    {
        _legalEntityService = legalEntityService;
        _auditService = auditService;
    }

    private string CurrentUser() =>
        RBBH.ConnectedParties.Helpers.AuthenticatedActor.Username(User);

    /// <summary>
    /// Vraća paginiranu listu svih aktivnih pravnih lica.
    /// </summary>
    /// <param name="page">Broj stranice (počinje od 1).</param>
    /// <param name="pageSize">Broj rezultata po stranici (10, 25, 50).</param>
    /// <param name="search">Pretraga po nazivu, poreznom broju ili FBA ID-u (case-insensitive).</param>
    [HttpGet]
    [Authorize(Policy = ApplicationPolicies.LegalPersonsRead)]
    [ProducesResponseType(typeof(LegalEntityListDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var result = await _legalEntityService.GetAllAsync(page, pageSize, search);
        return Ok(result);
    }

    [HttpGet("export")]
    [Authorize(Policy = ApplicationPolicies.LegalPersonsRead)]
    public async Task<IActionResult> Export()
    {
        var items = await _legalEntityService.GetAllForExportAsync();
        var bytes = RegistryExcelExporter.Create(
            "Pravna lica",
            ["Naziv", "Rezidentnost", "Porezni broj", "FBA ID", "Matbroj", "Osnov povezanosti", "Opis povezanosti", "Povezano lice s Bankom", "Datum od", "Datum do", "Matični broj registracije", "GCC broj", "GCC naziv", "Velika izloženost", "Status", "Kreirao", "Kreirano", "Izmijenio", "Izmijenjeno"],
            items.Select(item => (IReadOnlyList<object?>)
            [item.Name, item.IsResident ? "Rezident" : "Nerezident", item.TaxNumber, item.FbaId, item.Matbroj, item.BasisOfConnection, item.ConnectionDescription, item.ConnectedWithBank, item.DateFrom, item.DateTo, item.MaticniBroj, item.GccNumber, item.GccName, item.IsLargeExposure, item.Status, item.CreatedBy, item.CreatedAt, item.ModifiedBy, item.ModifiedAt]));
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"pravna-lica-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx");
    }

    /// <summary>
    /// Vraća detalje jednog pravnog lica po ID-u.
    /// </summary>
    /// <param name="id">ID pravnog lica.</param>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = ApplicationPolicies.LegalPersonsRead)]
    [ProducesResponseType(typeof(LegalEntityDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _legalEntityService.GetByIdAsync(id);

        if (entity is null)
            return NotFound(Err(BackendMessages.Message("validation.registry.legal.id.missing", id)));

        return Ok(entity);
    }
    /// <summary>
    /// Pretražuje pravna lica za potrebe forme Limita.
    /// Pretraga se vrši po nazivu i matičnom broju.
    /// </summary>
    /// <param name="search">Tekst pretrage — naziv ili matični broj.</param>
    [HttpGet("limits/search")]
    [Authorize(Policy = ApplicationPolicies.LimitsRead)]
    [ProducesResponseType(typeof(List<LegalEntityLookupDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SearchForLimits([FromQuery] string? search = null)
    {
        var result = await _legalEntityService.SearchForLimitsAsync(search);
        return Ok(result);
    }

    [HttpGet("limits/lookup"), Authorize(Policy = ApplicationPolicies.LimitsRead)]
    public async Task<IActionResult> PagedLookup(CancellationToken ct, string? search = null, int page = 1, int pageSize = 20) =>
        Ok(await _legalEntityService.SearchForLimitsPageAsync(search, page, pageSize, ct));

    /// <summary>
    /// Vraća podatke pravnog lica potrebne za automatsko popunjavanje forme Limita.
    /// </summary>
    /// <param name="id">ID pravnog lica.</param>
    [HttpGet("{id:guid}/limit-form-data")]
    [Authorize(Policy = ApplicationPolicies.LimitsRead)]
    [ProducesResponseType(typeof(LegalEntityLimitFormDataDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetLimitFormData(Guid id)
    {
        var result = await _legalEntityService.GetLimitFormDataAsync(id);

        if (result is null)
            return NotFound(Err(BackendMessages.Message("validation.registry.legal.id.missing", id)));

        return Ok(result);
    }

    /// <summary>
    /// Kreira novo pravno lice.
    /// Rezidentno: porezni broj obavezan (13 cifara).
    /// Nerezidentno: FBA ID obavezan, porezni broj nije aktivan.
    /// </summary>
    /// <param name="dto">Podaci novog pravnog lica.</param>
    [HttpPost]
    [Authorize(Policy = ApplicationPolicies.LegalPersonsCreate)]
    [ProducesResponseType(typeof(LegalEntityDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateLegalEntityDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _legalEntityService.CreateAsync(dto, CurrentUser());

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Ažurira postojeće pravno lice.
    /// Identifikacioni podaci (porezni broj / FBA ID) se ne mogu mijenjati.
    /// </summary>
    /// <param name="id">ID pravnog lica.</param>
    /// <param name="dto">Novi podaci.</param>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = ApplicationPolicies.LegalPersonsEdit)]
    [ProducesResponseType(typeof(LegalEntityDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLegalEntityDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updated = await _legalEntityService.UpdateAsync(id, dto, CurrentUser());

        return Ok(updated);
    }

    /// <summary>
    /// Deaktivira pravno lice (soft delete — IsActive = false).
    /// </summary>
    /// <param name="id">ID pravnog lica.</param>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = ApplicationPolicies.LegalPersonsDelete)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _legalEntityService.DeleteAsync(id, CurrentUser());

        return Ok(new { message = BackendMessages.Message("messages.registry.legal.deactivated") });
    }

    /// <summary>
    /// Uvozi pravna lica iz Excel fajla (.xlsx).
    /// Kolone: Naziv, Tip (Rezident/Nerezident), Porezni broj, FBA ID,
    /// Matbroj, Osnov povezanosti, Opis povezanosti, Pov. lice sa Bankom (DA/NE), Datum od, Datum do.
    /// Red 1 je zaglavlje i preskače se.
    /// </summary>
    [HttpPost("import")]
    [Authorize(Policy = ApplicationPolicies.LegalPersonsCreate)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportResultDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(Err(BackendMessages.Message("errors.workbook.required")));

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(Err(BackendMessages.Message("errors.workbook.format")));
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(Err(BackendMessages.Message("errors.workbook.size")));

        using var stream = file.OpenReadStream();
        var username = CurrentUser();
        var result = await _legalEntityService.ImportFromExcelAsync(stream, username);

        return Ok(result);
    }

    private static object Err(string message, string? field = null) =>
        new { errors = new[] { new { field, message } } };
}
