using RBBH.ConnectedParties.API.Controllers.BaseController;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO;
using RBBH.ConnectedParties.DL.DTO.RelatedPersons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RBBH.ConnectedParties.Helpers.Excel;

namespace RBBH.ConnectedParties.API.Controllers;

/// <summary>
/// API za evidenciju povezanih fizičkih lica i njihovih članova porodice.
///
/// Povezano fizičko lice se kreira u statusu Draft. Identifikacija zavisi od rezidentnosti:
/// - Rezident: obavezan JMBG (13 cifara, validna kontrolna cifra).
/// - Nerezident: JMBG nije obavezan; obavezan je broj pasoša ili FBA ID.
///
/// Članovi porodice se vežu za matično lice. Ako je postavljena izjava o nepostojanju
/// članova porodice (DeclarationNoFamilyMembers = true), unos novih članova nije dozvoljen.
/// Endpoint /family-tree vraća hijerarhijski prikaz porodice.
/// </summary>
[ApiController]
[Route("api/related-persons")]
[Authorize(Roles = "physical-persons")]
public class RelatedPersonController(
    IRelatedPersonService relatedPersonService,
    IAuditService auditService) : BaseResuItController
{
    private readonly IRelatedPersonService _relatedPersonService = relatedPersonService;
    private readonly IAuditService _auditService = auditService;

    // ─── RelatedPerson — READ ───────────────────────────────────────────────

    /// <summary>Vraća listu svih povezanih fizičkih lica (skraćeni prikaz).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RelatedPersonSummaryDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RelatedPersonSummaryDTO>>> GetAll()
    {
        var result = await _relatedPersonService.GetAll();
        return HandleResult(result);
    }

    /// <summary>Vraća listu svih povezanih fizičkih lica s punim podacima (za export).</summary>
    [HttpGet("detailed")]
    [ProducesResponseType(typeof(List<RelatedPersonResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RelatedPersonResponseDTO>>> GetAllDetailed()
    {
        var result = await _relatedPersonService.GetAllDetailed();
        return HandleResult(result);
    }

    /// <summary>Vraća jedno povezano fizičko lice po ID-u (puni prikaz).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RelatedPersonResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RelatedPersonResponseDTO>> GetById([FromRoute] Guid id)
    {
        var result = await _relatedPersonService.GetById(id);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/relationship-tree")]
    public async Task<ActionResult<List<RelatedPersonTreeNodeDTO>>> GetRelationshipTree([FromRoute] Guid id)
    {
        var result = await _relatedPersonService.GetRelationshipTree(id);
        return HandleResult(result);
    }

    [HttpGet("check-duplicate")]
    public async Task<ActionResult<DuplicateIdentityResponseDTO>> CheckDuplicate(
        [FromQuery] string? jmbg,
        [FromQuery] string? passportNumber,
        [FromQuery] string? fbaId,
        [FromQuery] Guid? excludeId = null)
        => Ok(await _relatedPersonService.CheckDuplicateIdentity(jmbg, passportNumber, fbaId, excludeId));

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var result = await _relatedPersonService.GetAllDetailed();
        if (!result.IsSuccessful) return HTTPExceptiontFromResult(result).Result!;
        var bytes = RegistryExcelExporter.Create(
            "Fizička lica",
            ["Tip", "Ime", "Prezime", "Rezidentnost", "JMBG", "Pasoš", "FBA ID", "GCC broj", "GCC naziv", "Osnov povezanosti", "Osnov posebnog odnosa", "Povezano lice", "Porodični odnos", "Datum od", "Datum do", "Status"],
            result.Value.Select(item => (IReadOnlyList<object?>)
            [item.PersonTypeLabel, item.FirstName, item.LastName, item.ResidencyLabel, item.JMBG, item.PassportNumber, item.FBAId, item.GCCNumber, item.GCCName, item.RelationBasis, item.SpecialRelationBasis, item.RelatedToPersonName, item.FamilyRelationshipTypeLabel, item.DateFrom, item.DateTo, item.StatusLabel]));
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"fizicka-lica-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx");
    }

    // ─── RelatedPerson — CREATE / UPDATE / DELETE ───────────────────────────

    /// <summary>
    /// Kreira novo povezano fizičko lice. Novi zapis se uvijek čuva u statusu Draft.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RelatedPersonResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RelatedPersonResponseDTO>> Create([FromBody] CreateRelatedPersonDTO dto)
    {
        var korisnik = GetKorisnik();
        var result = await _relatedPersonService.Create(dto, korisnik);

        if (result.IsSuccessful)
            return Created($"{Request.Path}/{result.Value.Id}", result.Value);

        return HTTPExceptiontFromResult(result);
    }

    /// <summary>Ažurira postojeće povezano fizičko lice.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RelatedPersonResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RelatedPersonResponseDTO>> Update([FromRoute] Guid id, [FromBody] UpdateRelatedPersonDTO dto)
    {
        var korisnik = GetKorisnik();
        var result = await _relatedPersonService.Update(id, dto, korisnik);
        return HandleResult(result);
    }

    /// <summary>Soft-delete povezanog fizičkog lica (i svih njegovih aktivnih članova porodice).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete([FromRoute] Guid id)
    {
        var korisnik = GetKorisnik();
        var result = await _relatedPersonService.Delete(id, korisnik);

        if (result.IsSuccessful)
            return Ok(new { message = "Povezano fizičko lice je uspješno obrisano." });

        return HTTPExceptiontFromResult(result).Result!;
    }

    // ─── FamilyMember — READ ─────────────────────────────────────────────────

    /// <summary>Vraća sve aktivne članove porodice za sva matična lica — koristi se za export.</summary>
    [HttpGet("all-family-members")]
    [ProducesResponseType(typeof(List<FamilyMemberResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FamilyMemberResponseDTO>>> GetAllFamilyMembers()
    {
        var result = await _relatedPersonService.GetAllFamilyMembers();
        return HandleResult(result);
    }

    /// <summary>Vraća ravnu listu članova porodice za dato matično lice.</summary>
    [HttpGet("{id:guid}/family-members")]
    [ProducesResponseType(typeof(List<FamilyMemberResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<FamilyMemberResponseDTO>>> GetFamilyMembers([FromRoute] Guid id)
    {
        var result = await _relatedPersonService.GetFamilyMembers(id);
        return HandleResult(result);
    }

    /// <summary>
    /// Vraća porodično stablo za dato matično lice — članovi porodice organizovani
    /// hijerarhijski (korijenski čvorovi sa rekurzivno popunjenim Children).
    /// </summary>
    [HttpGet("{id:guid}/family-tree")]
    [ProducesResponseType(typeof(List<FamilyMemberResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<FamilyMemberResponseDTO>>> GetFamilyTree([FromRoute] Guid id)
    {
        var result = await _relatedPersonService.GetFamilyTree(id);
        return HandleResult(result);
    }

    // ─── FamilyMember — CREATE / UPDATE / DELETE ────────────────────────────

    /// <summary>
    /// Dodaje novog člana porodice matičnom licu.
    /// Vraća HTTP 400 ako matično lice ima postavljenu izjavu o nepostojanju članova porodice.
    /// </summary>
    [HttpPost("{id:guid}/family-members")]
    [ProducesResponseType(typeof(FamilyMemberResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FamilyMemberResponseDTO>> AddFamilyMember([FromRoute] Guid id, [FromBody] CreateFamilyMemberDTO dto)
    {
        var korisnik = GetKorisnik();
        var result = await _relatedPersonService.AddFamilyMember(id, dto, korisnik);

        if (result.IsSuccessful)
            return Created($"{Request.Path}/{result.Value.Id}", result.Value);

        return HTTPExceptiontFromResult(result);
    }

    /// <summary>Ažurira podatke postojećeg člana porodice.</summary>
    [HttpPut("{id:guid}/family-members/{familyMemberId:guid}")]
    [ProducesResponseType(typeof(FamilyMemberResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FamilyMemberResponseDTO>> UpdateFamilyMember(
        [FromRoute] Guid id,
        [FromRoute] Guid familyMemberId,
        [FromBody] UpdateFamilyMemberDTO dto)
    {
        var korisnik = GetKorisnik();
        var result = await _relatedPersonService.UpdateFamilyMember(id, familyMemberId, dto, korisnik);
        return HandleResult(result);
    }

    /// <summary>Soft-delete člana porodice.</summary>
    [HttpDelete("{id:guid}/family-members/{familyMemberId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteFamilyMember([FromRoute] Guid id, [FromRoute] Guid familyMemberId)
    {
        var korisnik = GetKorisnik();
        var result = await _relatedPersonService.DeleteFamilyMember(id, familyMemberId, korisnik);

        if (result.IsSuccessful)
            return Ok(new { message = "Član porodice je uspješno obrisan." });

        return HTTPExceptiontFromResult(result).Result!;
    }

    // ─── Import ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Uvozi fizička lica iz Excel fajla (.xlsx).
    /// Kolone: Ime, Prezime, Rezidentnost, JMBG, Broj pasoša, FBA ID, GCC broj, GCC naziv,
    /// Osnov povezanosti, Osnov posebnog odnosa, Datum od, Datum do,
    /// Izjava bez clanova porodice (DA/NE), Pov. lice sa Bankom (DA/NE),
    /// Lice u posebnom odnosu (DA/NE), Poseban ugovor (DA/NE), Malus &amp; Clawback (DA/NE).
    /// Red 1 je zaglavlje i preskače se.
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImportResultDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Fajl nije priložen ili je prazan." });

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Dozvoljen je samo .xlsx format." });
        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "Excel datoteka smije imati najviše 10 MB." });

        using var stream = file.OpenReadStream();
        var korisnik = GetKorisnik();
        var result = await _relatedPersonService.ImportFromExcelAsync(stream, korisnik);

        await _auditService.LogAsync(new AuditEntry
        {
            TableName = "RelatedPerson",
            RecordId = "-",
            Action = "IMPORT",
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                fileName = file.FileName,
                imported = result.Imported,
                failed = result.Failed
            }),
            UserId = korisnik,
            Username = korisnik,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        return Ok(result);
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Čita korisničko ime iz JWT claimova, ili fallback na host name.
    /// </summary>
    private string GetKorisnik()
    {
        return User.Identity?.Name
            ?? User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
            ?? User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
            ?? Environment.MachineName;
    }
}
