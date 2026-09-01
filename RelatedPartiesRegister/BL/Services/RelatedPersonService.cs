using System.Globalization;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO;
using RBBH.ConnectedParties.DL.DTO.RelatedPersons;
using RBBH.ConnectedParties.DL.Entities.RelatedPersons;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions.Validations;
using RBBH.ConnectedParties.Helpers.Validators;
using RBBH.ConnectedParties.Helpers.Excel;
using RBBH.ConnectedParties.Helpers.Policies;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>
/// Implementacija servisa za evidenciju povezanih fizičkih lica i članova porodice.
/// </summary>
public class RelatedPersonService(
    ConnectedPartiesDbContext dbContext,
    IEmailService emailService,
    IOptions<EmailSettings> emailSettings) : IRelatedPersonService
{
    private static readonly ExcelImportSchema.Column[] ImportColumns =
    [
        new("Ime / First name", "Ime", "First name"),
        new("Prezime / Last name", "Prezime", "Last name"),
        new("Rezidentnost / Residency", "Rezidentnost", "Residency"),
        new("JMBG / National ID", "JMBG", "National ID"),
        new("Broj pasoša / Passport number", "Broj pasoša", "Passport number"),
        new("FBA ID", "FBA ID"),
        new("GCC broj / GCC number", "GCC broj", "GCC number"),
        new("GCC naziv / GCC name", "GCC naziv", "GCC name"),
        new("Osnov povezanosti / Relation basis", "Osnov povezanosti", "Relation basis"),
        new("Osnov posebnog odnosa / Special relationship basis", "Osnov posebnog odnosa", "Special relationship basis"),
        new("Datum od / Date from", "Datum od", "Date from"),
        new("Datum do / Date to", "Datum do", "Date to"),
        new("Izjava bez članova porodice / No family members", "Izjava bez članova porodice", "Izjava o nepostojanju članova porodice", "No family members"),
        new("Povezano lice s Bankom / Related to the Bank", "Pov. lice sa Bankom", "Povezano lice s Bankom", "Related to the Bank"),
        new("Poseban odnos s Bankom / Special relationship with the Bank", "Lice u posebnom odnosu sa Bankom", "Poseban odnos s Bankom", "Special relationship with the Bank"),
        new("Poseban ugovor / Special contract", "Poseban ugovor", "Special contract"),
        new("Malus & Clawback", "Malus & Clawback")
    ];
    private readonly ConnectedPartiesDbContext _dbContext = dbContext;
    private readonly IEmailService _emailService = emailService;
    private readonly EmailSettings _emailSettings = emailSettings.Value;

    // ─── RelatedPerson — READ ───────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<List<RelatedPersonSummaryDTO>>> GetAll()
    {
        var items = await _dbContext.RelatedPersons
            .AsNoTracking()
            .Where(rp => rp.IsActive)
            .OrderByDescending(rp => rp.CreatedAt)
            .ProjectToType<RelatedPersonSummaryDTO>()
            .ToListAsync();

        return Result<List<RelatedPersonSummaryDTO>>.Success(items);
    }

    /// <inheritdoc/>
    public async Task<Result<List<RelatedPersonResponseDTO>>> GetAllDetailed()
    {
        var items = await _dbContext.RelatedPersons
            .AsNoTracking()
            .Where(rp => rp.IsActive)
            .OrderByDescending(rp => rp.CreatedAt)
            .ProjectToType<RelatedPersonResponseDTO>()
            .ToListAsync();

        return Result<List<RelatedPersonResponseDTO>>.Success(items);
    }

    /// <inheritdoc/>
    public async Task<Result<RelatedPersonResponseDTO>> GetById(Guid id)
    {
        if (id == Guid.Empty)
            return Result<RelatedPersonResponseDTO>.ValidationError("ID nije validan.");

        var entity = await _dbContext.RelatedPersons
            .AsNoTracking()
            .Include(rp => rp.RelatedToPerson)
            .FirstOrDefaultAsync(rp => rp.Id == id && rp.IsActive);

        if (entity is null)
            return Result<RelatedPersonResponseDTO>.NotFoundError($"Povezano fizičko lice s ID={id} nije pronađeno.");

        return Result<RelatedPersonResponseDTO>.Success(entity.Adapt<RelatedPersonResponseDTO>());
    }

    public async Task<Result<List<RelatedPersonTreeNodeDTO>>> GetRelationshipTree(Guid relatedPersonId)
    {
        var people = await _dbContext.RelatedPersons
            .AsNoTracking()
            .Where(person => person.IsActive)
            .OrderBy(person => person.FirstName)
            .ThenBy(person => person.LastName)
            .ToListAsync();

        var selected = people.FirstOrDefault(person => person.Id == relatedPersonId);
        if (selected is null)
            return Result<List<RelatedPersonTreeNodeDTO>>.NotFoundError("Odabrano fizičko lice nije pronađeno.");

        var byId = people.ToDictionary(person => person.Id);
        var root = selected;
        var visited = new HashSet<Guid>();
        while (root.RelatedToPersonId.HasValue
               && visited.Add(root.Id)
               && byId.TryGetValue(root.RelatedToPersonId.Value, out var parent))
            root = parent;

        RelatedPersonTreeNodeDTO Build(RelatedPerson person, HashSet<Guid> path)
        {
            var node = new RelatedPersonTreeNodeDTO
            {
                Id = person.Id,
                FirstName = person.FirstName,
                LastName = person.LastName,
                PersonType = person.RelatedToPersonId.HasValue ? "FamilyMember" : person.IsIdentifiedStaff ? "Employee" : "RelatedPerson",
                PersonTypeLabel = person.RelatedToPersonId.HasValue ? "Član porodice" : person.IsIdentifiedStaff ? "Zaposlenik" : "Povezano lice",
                RelationshipType = person.FamilyRelationshipType
            };

            if (!path.Add(person.Id)) return node;
            node.Children = people
                .Where(candidate => candidate.RelatedToPersonId == person.Id)
                .Select(candidate => Build(candidate, new HashSet<Guid>(path)))
                .ToList();
            return node;
        }

        return Result<List<RelatedPersonTreeNodeDTO>>.Success([Build(root, [])]);
    }

    public async Task<DuplicateIdentityResponseDTO> CheckDuplicateIdentity(
        string? jmbg,
        string? passportNumber,
        string? fbaId,
        Guid? excludeId = null)
    {
        var normalizedJmbg = NormalizeIdentifier(jmbg);
        var normalizedPassport = NormalizeIdentifier(passportNumber)?.ToUpperInvariant();
        var normalizedFba = NormalizeIdentifier(fbaId)?.ToUpperInvariant();
        var query = _dbContext.RelatedPersons.AsNoTracking().Where(person => !excludeId.HasValue || person.Id != excludeId.Value);

        if (normalizedJmbg is not null && await query.AnyAsync(person => person.JMBG == normalizedJmbg))
            return Duplicate("jmbg", "Fizičko lice sa ovim JMBG-om već postoji.");
        if (normalizedPassport is not null && await query.AnyAsync(person => person.PassportNumber != null && person.PassportNumber.ToUpper() == normalizedPassport))
            return Duplicate("passportNumber", "Fizičko lice sa ovim brojem pasoša već postoji.");
        if (normalizedFba is not null && await query.AnyAsync(person => person.FBAId != null && person.FBAId.ToUpper() == normalizedFba))
            return Duplicate("fbaId", "Fizičko lice sa ovim FBA ID-em već postoji.");
        return new DuplicateIdentityResponseDTO();
    }

    private static DuplicateIdentityResponseDTO Duplicate(string field, string message) =>
        new() { Exists = true, Field = field, Message = message };

    private static string? NormalizeIdentifier(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // ─── RelatedPerson — CREATE / UPDATE / DELETE ───────────────────────────

    /// <inheritdoc/>
    public async Task<Result<RelatedPersonResponseDTO>> Create(CreateRelatedPersonDTO dto, string korisnik)
    {
        ImmediateFamilyPolicy.Apply(dto);
        var validationError = RelatedPersonValidator.Validate(dto);
        if (validationError is not null)
            return Result<RelatedPersonResponseDTO>.ValidationError(validationError);

        var duplicate = await CheckDuplicateIdentity(dto.JMBG, dto.PassportNumber, dto.FBAId);
        if (duplicate.Exists)
            return Result<RelatedPersonResponseDTO>.ValidationError(duplicate.Message!);

        var linkError = await ValidateFamilyLink(dto.RelatedToPersonId, null);
        if (linkError is not null)
            return Result<RelatedPersonResponseDTO>.ValidationError(linkError);

        var entity = dto.Adapt<RelatedPerson>();
        entity.Id = Guid.NewGuid();
        entity.Status = RelatedPersonStatus.Draft;
        entity.IsActive = true;
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedBy = korisnik;

        // Normalizacija JMBG-a (uklanjanje praznina)
        if (!string.IsNullOrWhiteSpace(entity.JMBG))
            entity.JMBG = entity.JMBG.Trim();

        _dbContext.RelatedPersons.Add(entity);
        await _dbContext.SaveChangesAsync();

        await _emailService.SendHrNewPhysicalPersonAsync(
            _emailSettings.HrEmail,
            $"{entity.FirstName} {entity.LastName}",
            korisnik,
            entity.RelationBasis ?? "—",
            entity.DateFrom,
            entity.DateTo);

        return Result<RelatedPersonResponseDTO>.Success(entity.Adapt<RelatedPersonResponseDTO>());
    }

    /// <inheritdoc/>
    public async Task<Result<RelatedPersonResponseDTO>> Update(Guid id, UpdateRelatedPersonDTO dto, string korisnik)
    {
        if (id == Guid.Empty)
            return Result<RelatedPersonResponseDTO>.ValidationError("ID nije validan.");

        ImmediateFamilyPolicy.Apply(dto);
        var validationError = RelatedPersonValidator.Validate(dto);
        if (validationError is not null)
            return Result<RelatedPersonResponseDTO>.ValidationError(validationError);

        var duplicate = await CheckDuplicateIdentity(dto.JMBG, dto.PassportNumber, dto.FBAId, id);
        if (duplicate.Exists)
            return Result<RelatedPersonResponseDTO>.ValidationError(duplicate.Message!);

        var linkError = await ValidateFamilyLink(dto.RelatedToPersonId, id);
        if (linkError is not null)
            return Result<RelatedPersonResponseDTO>.ValidationError(linkError);

        var entity = await _dbContext.RelatedPersons
            .AsTracking()
            .FirstOrDefaultAsync(rp => rp.Id == id);

        if (entity is null)
            return Result<RelatedPersonResponseDTO>.NotFoundError($"Povezano fizičko lice s ID={id} nije pronađeno.");

        dto.Adapt(entity);
        entity.JMBG = string.IsNullOrWhiteSpace(entity.JMBG) ? entity.JMBG : entity.JMBG.Trim();
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = korisnik;

        await _dbContext.SaveChangesAsync();

        if (entity.DateTo.HasValue && entity.DateTo.Value.Date < DateTime.UtcNow.Date)
        {
            await _emailService.SendHrPhysicalPersonExpiredAsync(
                _emailSettings.HrEmail,
                $"{entity.FirstName} {entity.LastName}",
                korisnik,
                entity.RelationBasis ?? "—",
                entity.DateTo.Value);
        }

        return Result<RelatedPersonResponseDTO>.Success(entity.Adapt<RelatedPersonResponseDTO>());
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> Delete(Guid id, string korisnik)
    {
        if (id == Guid.Empty)
            return Result<bool>.ValidationError("ID nije validan.");

        var entity = await _dbContext.RelatedPersons
            .Include(rp => rp.RelatedFamilyMembers)
            .AsTracking()
            .FirstOrDefaultAsync(rp => rp.Id == id);

        if (entity is null)
            return Result<bool>.NotFoundError($"Povezano fizičko lice s ID={id} nije pronađeno.");

        var now = DateTime.UtcNow;

        // Soft-delete matičnog lica
        entity.IsActive = false;
        entity.ModifiedAt = now;
        entity.ModifiedBy = korisnik;

        // Povezana lica ostaju u registru; uklanja se samo veza prema obrisanom licu.
        foreach (var member in entity.RelatedFamilyMembers.Where(member => member.IsActive))
        {
            member.RelatedToPersonId = null;
            member.FamilyRelationshipType = null;
            member.ModifiedAt = now;
            member.ModifiedBy = korisnik;
        }

        await _dbContext.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    /// <inheritdoc/>
    public async Task<Result<RelatedPersonResponseDTO>> Verify(Guid id, string korisnik)
    {
        if (id == Guid.Empty)
            return Result<RelatedPersonResponseDTO>.ValidationError("ID nije validan.");

        var entity = await _dbContext.RelatedPersons
            .AsTracking()
            .FirstOrDefaultAsync(rp => rp.Id == id && rp.IsActive);

        if (entity is null)
            return Result<RelatedPersonResponseDTO>.NotFoundError($"Povezano fizičko lice s ID={id} nije pronađeno.");

        if (entity.Status == RelatedPersonStatus.Verified)
            return Result<RelatedPersonResponseDTO>.ValidationError("Povezano fizičko lice je već verificirano.");

        entity.Status = RelatedPersonStatus.Verified;
        entity.VerifiedBy = korisnik;
        entity.VerifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = korisnik;
        entity.ModifiedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Result<RelatedPersonResponseDTO>.Success(entity.Adapt<RelatedPersonResponseDTO>());
    }

    private async Task<string?> ValidateFamilyLink(Guid? relatedToPersonId, Guid? currentPersonId)
    {
        if (!relatedToPersonId.HasValue)
            return null;
        if (currentPersonId.HasValue && relatedToPersonId.Value == currentPersonId.Value)
            return "Fizičko lice ne može biti povezano samo sa sobom.";

        var exists = await _dbContext.RelatedPersons
            .AsNoTracking()
            .AnyAsync(person => person.Id == relatedToPersonId.Value && person.IsActive);
        if (!exists)
            return "Odabrano povezano fizičko lice više ne postoji ili nije aktivno.";

        if (currentPersonId.HasValue && await CreatesRelatedPersonCycle(currentPersonId.Value, relatedToPersonId.Value))
            return "Odabrana porodična veza bi kreirala kružnu vezu između fizičkih lica.";

        return null;
    }

    private async Task<bool> CreatesRelatedPersonCycle(Guid currentPersonId, Guid candidateParentId)
    {
        var visited = new HashSet<Guid>();
        Guid? cursor = candidateParentId;
        while (cursor.HasValue && visited.Add(cursor.Value))
        {
            if (cursor.Value == currentPersonId)
                return true;

            cursor = await _dbContext.RelatedPersons
                .AsNoTracking()
                .Where(person => person.Id == cursor.Value && person.IsActive)
                .Select(person => person.RelatedToPersonId)
                .SingleOrDefaultAsync();
        }

        return false;
    }

    // ─── FamilyMember — READ ─────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<List<FamilyMemberResponseDTO>>> GetAllFamilyMembers()
    {
        var members = await _dbContext.FamilyMembers
            .AsNoTracking()
            .Where(fm => fm.IsActive)
            .OrderBy(fm => fm.RelatedPersonId)
            .ThenBy(fm => fm.CreatedAt)
            .ToListAsync();

        return Result<List<FamilyMemberResponseDTO>>.Success(
            members.Adapt<List<FamilyMemberResponseDTO>>());
    }

    /// <inheritdoc/>
    public async Task<Result<List<FamilyMemberResponseDTO>>> GetFamilyMembers(Guid relatedPersonId)
    {
        var personExists = await RelatedPersonExists(relatedPersonId);
        if (!personExists)
            return Result<List<FamilyMemberResponseDTO>>.NotFoundError(
                $"Povezano fizičko lice s ID={relatedPersonId} nije pronađeno.");

        var members = await _dbContext.FamilyMembers
            .AsNoTracking()
            .Where(fm => fm.RelatedPersonId == relatedPersonId)
            .OrderBy(fm => fm.CreatedAt)
            .ToListAsync();

        return Result<List<FamilyMemberResponseDTO>>.Success(
            members.Adapt<List<FamilyMemberResponseDTO>>());
    }

    /// <inheritdoc/>
    public async Task<Result<List<FamilyMemberResponseDTO>>> GetFamilyTree(Guid relatedPersonId)
    {
        var personExists = await RelatedPersonExists(relatedPersonId);
        if (!personExists)
            return Result<List<FamilyMemberResponseDTO>>.NotFoundError(
                $"Povezano fizičko lice s ID={relatedPersonId} nije pronađeno.");

        var members = await _dbContext.FamilyMembers
            .AsNoTracking()
            .Where(fm => fm.RelatedPersonId == relatedPersonId)
            .OrderBy(fm => fm.CreatedAt)
            .ToListAsync();

        var dtos = members.Adapt<List<FamilyMemberResponseDTO>>();
        var tree = BuildFamilyTree(dtos);

        return Result<List<FamilyMemberResponseDTO>>.Success(tree);
    }

    // ─── FamilyMember — CREATE / UPDATE / DELETE ────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<FamilyMemberResponseDTO>> AddFamilyMember(Guid relatedPersonId, CreateFamilyMemberDTO dto, string korisnik)
    {
        var relatedPerson = await _dbContext.RelatedPersons
            .Include(rp => rp.FamilyMembers)
            .FirstOrDefaultAsync(rp => rp.Id == relatedPersonId);

        if (relatedPerson is null)
            return Result<FamilyMemberResponseDTO>.NotFoundError(
                $"Povezano fizičko lice s ID={relatedPersonId} nije pronađeno.");

        var declarationError = RelatedPersonValidator.ValidateCanAddFamilyMember(relatedPerson);
        if (declarationError is not null)
            return Result<FamilyMemberResponseDTO>.ValidationError(declarationError);

        var validationError = RelatedPersonValidator.Validate(dto);
        if (validationError is not null)
            return Result<FamilyMemberResponseDTO>.ValidationError(validationError);

        // Ako je naveden ParentFamilyMemberId, mora pripadati istom matičnom licu
        if (dto.ParentFamilyMemberId.HasValue)
        {
            var parentExists = relatedPerson.FamilyMembers
                .Any(fm => fm.Id == dto.ParentFamilyMemberId.Value && fm.IsActive);

            if (!parentExists)
                return Result<FamilyMemberResponseDTO>.ValidationError(
                    "Odabrani nadređeni član porodice (ParentFamilyMemberId) ne postoji za ovo matično lice.");
        }

        var entity = dto.Adapt<FamilyMember>();
        entity.Id = Guid.NewGuid();
        entity.RelatedPersonId = relatedPersonId;
        entity.IsActive = true;
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedBy = korisnik;

        if (!string.IsNullOrWhiteSpace(entity.JMBG))
            entity.JMBG = entity.JMBG.Trim();

        _dbContext.FamilyMembers.Add(entity);
        await _dbContext.SaveChangesAsync();

        return Result<FamilyMemberResponseDTO>.Success(entity.Adapt<FamilyMemberResponseDTO>());
    }

    /// <inheritdoc/>
    public async Task<Result<FamilyMemberResponseDTO>> UpdateFamilyMember(Guid relatedPersonId, Guid familyMemberId, UpdateFamilyMemberDTO dto, string korisnik)
    {
        var validationError = RelatedPersonValidator.Validate(dto);
        if (validationError is not null)
            return Result<FamilyMemberResponseDTO>.ValidationError(validationError);

        var entity = await _dbContext.FamilyMembers
            .AsTracking()
            .FirstOrDefaultAsync(fm => fm.Id == familyMemberId && fm.RelatedPersonId == relatedPersonId);

        if (entity is null)
            return Result<FamilyMemberResponseDTO>.NotFoundError(
                $"Član porodice s ID={familyMemberId} nije pronađen za matično lice ID={relatedPersonId}.");

        // Validacija ParentFamilyMemberId: ne smije pokazivati na samog sebe niti
        // kreirati ciklus, i mora pripadati istom matičnom licu.
        if (dto.ParentFamilyMemberId.HasValue)
        {
            if (dto.ParentFamilyMemberId.Value == entity.Id)
                return Result<FamilyMemberResponseDTO>.ValidationError(
                    "Član porodice ne može biti naveden kao nadređeni sam sebi.");

            var parentExists = await _dbContext.FamilyMembers
                .AnyAsync(fm => fm.Id == dto.ParentFamilyMemberId.Value
                                && fm.RelatedPersonId == relatedPersonId
                                && fm.IsActive);

            if (!parentExists)
                return Result<FamilyMemberResponseDTO>.ValidationError(
                    "Odabrani nadređeni član porodice (ParentFamilyMemberId) ne postoji za ovo matično lice.");

            var wouldCreateCycle = await CreatesCycle(entity.Id, dto.ParentFamilyMemberId.Value);
            if (wouldCreateCycle)
                return Result<FamilyMemberResponseDTO>.ValidationError(
                    "Odabrana veza bi kreirala kružnu zavisnost u porodičnom stablu.");
        }

        dto.Adapt(entity);

        if (!string.IsNullOrWhiteSpace(entity.JMBG))
            entity.JMBG = entity.JMBG.Trim();

        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = korisnik;

        await _dbContext.SaveChangesAsync();

        return Result<FamilyMemberResponseDTO>.Success(entity.Adapt<FamilyMemberResponseDTO>());
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> DeleteFamilyMember(Guid relatedPersonId, Guid familyMemberId, string korisnik)
    {
        var entity = await _dbContext.FamilyMembers
            .Include(fm => fm.ChildFamilyMembers)
            .AsTracking()
            .FirstOrDefaultAsync(fm => fm.Id == familyMemberId && fm.RelatedPersonId == relatedPersonId);

        if (entity is null)
            return Result<bool>.NotFoundError(
                $"Član porodice s ID={familyMemberId} nije pronađen za matično lice ID={relatedPersonId}.");

        if (entity.ChildFamilyMembers.Any(c => c.IsActive))
            return Result<bool>.ValidationError(
                "Nije moguće obrisati člana porodice koji ima povezane potomke u porodičnom stablu. " +
                "Prvo uklonite ili premjestite potomke.");

        entity.IsActive = false;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = korisnik;

        await _dbContext.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    // ─── Import ─────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<ImportResultDTO> ImportFromExcelAsync(Stream stream, string createdBy)
    {
        using var wb = new ClosedXML.Excel.XLWorkbook(stream);
        var ws = wb.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        var result = new ImportResultDTO();
        var schemaErrors = ExcelImportSchema.Validate(ws, ImportColumns);
        if (schemaErrors.Count > 0)
        {
            result.Failed = Math.Max(lastRow - 1, 1);
            result.Errors.AddRange(schemaErrors);
            return result;
        }
        var usedJmbgs = (await _dbContext.RelatedPersons.AsNoTracking()
            .Where(person => person.JMBG != null)
            .Select(person => person.JMBG!)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var usedFbaIds = (await _dbContext.RelatedPersons.AsNoTracking()
            .Where(person => person.FBAId != null)
            .Select(person => person.FBAId!)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (int row = 2; row <= lastRow; row++)
        {
            try
            {
                var firstName     = ws.Cell(row, 1).GetString().Trim();
                var lastName      = ws.Cell(row, 2).GetString().Trim();
                var residencyStr  = ws.Cell(row, 3).GetString().Trim();
                var jmbg          = ws.Cell(row, 4).GetString().Trim();
                var passport      = ws.Cell(row, 5).GetString().Trim();
                var fbaId         = ws.Cell(row, 6).GetString().Trim();
                var gccNumber     = ws.Cell(row, 7).GetString().Trim();
                var gccName       = ws.Cell(row, 8).GetString().Trim();
                var relationBasis = ws.Cell(row, 9).GetString().Trim();
                var specialBasis  = ws.Cell(row, 10).GetString().Trim();
                var dateFromStr   = ws.Cell(row, 11).GetString().Trim();
                var dateToStr     = ws.Cell(row, 12).GetString().Trim();
                var noFamilyStr   = ws.Cell(row, 13).GetString().Trim();
                var connBankStr   = ws.Cell(row, 14).GetString().Trim();
                var specialRelStr = ws.Cell(row, 15).GetString().Trim();
                var specContStr   = ws.Cell(row, 16).GetString().Trim();
                var malusStr      = ws.Cell(row, 17).GetString().Trim();

                if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
                    continue; // prazan red — preskoči bez greške

                if (string.IsNullOrWhiteSpace(firstName))
                    throw new InvalidOperationException("Ime je obavezno.");
                if (string.IsNullOrWhiteSpace(lastName))
                    throw new InvalidOperationException("Prezime je obavezno.");

                var isResident = residencyStr.Equals("Rezident", StringComparison.OrdinalIgnoreCase) ||
                    residencyStr.Equals("Resident", StringComparison.OrdinalIgnoreCase);
                var isNonResident = residencyStr.Equals("Nerezident", StringComparison.OrdinalIgnoreCase) ||
                    residencyStr.Equals("Non-resident", StringComparison.OrdinalIgnoreCase) ||
                    residencyStr.Equals("Nonresident", StringComparison.OrdinalIgnoreCase);
                if (!isResident && !isNonResident)
                    throw new InvalidOperationException("Rezidentnost mora biti 'Rezident/Resident' ili 'Nerezident/Non-resident'.");
                var residency = isResident ? ResidencyType.Resident : ResidencyType.NonResident;

                DateTime? dateFrom = ParseOptionalDate(dateFromStr, "Datum od");
                DateTime? dateTo   = ParseOptionalDate(dateToStr, "Datum do");

                var dto = new CreateRelatedPersonDTO
                {
                    FirstName    = firstName,
                    LastName     = lastName,
                    Residency    = residency,
                    JMBG         = string.IsNullOrWhiteSpace(jmbg) ? null : jmbg,
                    PassportNumber = string.IsNullOrWhiteSpace(passport) ? null : passport,
                    FBAId        = string.IsNullOrWhiteSpace(fbaId) ? null : fbaId,
                    GCCNumber    = string.IsNullOrWhiteSpace(gccNumber) ? null : gccNumber,
                    GCCName      = string.IsNullOrWhiteSpace(gccName) ? null : gccName,
                    RelationBasis      = string.IsNullOrWhiteSpace(relationBasis) ? null : relationBasis,
                    RelationDescription = string.IsNullOrWhiteSpace(relationBasis) ? null : relationBasis,
                    SpecialRelationBasis = string.IsNullOrWhiteSpace(specialBasis) ? null : specialBasis,
                    DateFrom     = dateFrom,
                    DateTo       = dateTo,
                    IsIdentifiedStaff = true,
                    DeclarationNoFamilyMembers  = ParseRequiredBool(noFamilyStr, "Izjava o nepostojanju članova porodice"),
                    ConnectedWithBank            = ParseRequiredBool(connBankStr, "Povezano lice sa Bankom"),
                    SpecialRelationshipWithBank  = ParseRequiredBool(specialRelStr, "Lice u posebnom odnosu sa Bankom"),
                    SpecialContract              = ParseRequiredBool(specContStr, "Poseban ugovor"),
                    MalusClawback                 = ParseRequiredBool(malusStr, "Malus & Clawback")
                };
                var validationError = RelatedPersonValidator.Validate(dto);
                if (validationError is not null) throw new InvalidOperationException(validationError);
                if (dto.Residency == ResidencyType.Resident && !usedJmbgs.Add(dto.JMBG!))
                    throw new InvalidOperationException("Fizičko lice s ovim JMBG-om već postoji.");
                if (dto.Residency == ResidencyType.NonResident && !usedFbaIds.Add(dto.FBAId!))
                    throw new InvalidOperationException("Fizičko lice s ovim FBA ID-em već postoji.");

                var entity = new RelatedPerson
                {
                    Id = Guid.NewGuid(),
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Residency = dto.Residency,
                    JMBG = dto.JMBG,
                    PassportNumber = dto.PassportNumber,
                    FBAId = dto.FBAId,
                    GCCNumber = dto.GCCNumber,
                    GCCName = dto.GCCName,
                    RelationBasis = dto.RelationBasis,
                    RelationDescription = dto.RelationDescription,
                    SpecialRelationBasis = dto.SpecialRelationBasis,
                    DateFrom = dto.DateFrom,
                    DateTo = dto.DateTo,
                    IsIdentifiedStaff = dto.IsIdentifiedStaff,
                    DeclarationNoFamilyMembers = dto.DeclarationNoFamilyMembers,
                    ConnectedWithBank = dto.ConnectedWithBank,
                    SpecialRelationshipWithBank = dto.SpecialRelationshipWithBank,
                    SpecialContract = dto.SpecialContract,
                    MalusClawback = dto.MalusClawback,
                    Status       = RelatedPersonStatus.Draft,
                    IsActive     = true,
                    CreatedAt    = DateTime.UtcNow,
                    CreatedBy    = createdBy
                };

                _dbContext.RelatedPersons.Add(entity);
                result.Imported++;
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"Red {row}: {ex.Message}");
            }
        }

        if (result.Imported > 0)
            await _dbContext.SaveChangesAsync();

        return result;
    }

    private static bool ParseRequiredBool(string value, string field)
    {
        if (value.Equals("DA", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("YES", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
        if (value.Equals("NE", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("NO", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
        throw new InvalidOperationException($"Kolona '{field}' mora sadržavati DA/NE ili YES/NO.");
    }

    private static DateTime? ParseOptionalDate(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParse(value, CultureInfo.GetCultureInfo("bs-BA"), DateTimeStyles.None, out var date) ||
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return date;
        throw new InvalidOperationException($"Kolona '{field}' ne sadrži ispravan datum.");
    }

    // ─── Privatne pomoćne metode ─────────────────────────────────────────────

    private async Task<bool> RelatedPersonExists(Guid relatedPersonId)
    {
        return await _dbContext.RelatedPersons.AnyAsync(rp => rp.Id == relatedPersonId);
    }

    /// <summary>
    /// Provjerava da li bi postavljanje <paramref name="newParentId"/> kao roditelja
    /// za <paramref name="memberId"/> kreiralo ciklus u porodičnom stablu
    /// (tj. da li je <paramref name="memberId"/> negdje u lancu predaka od <paramref name="newParentId"/>).
    /// </summary>
    private async Task<bool> CreatesCycle(Guid memberId, Guid newParentId)
    {
        var currentId = (Guid?)newParentId;
        var visited = new HashSet<Guid>();

        while (currentId.HasValue)
        {
            if (currentId.Value == memberId)
                return true;

            if (!visited.Add(currentId.Value))
                break; // već postojeći ciklus u podacima — prekini da izbjegnemo beskonačnu petlju

            currentId = await _dbContext.FamilyMembers
                .Where(fm => fm.Id == currentId.Value)
                .Select(fm => fm.ParentFamilyMemberId)
                .FirstOrDefaultAsync();
        }

        return false;
    }

    /// <summary>
    /// Gradi hijerarhijsko stablo od ravne liste članova porodice.
    /// Vraća samo korijenske čvorove (ParentFamilyMemberId == null), s rekurzivno
    /// popunjenim <see cref="FamilyMemberResponseDTO.Children"/>.
    /// </summary>
    private static List<FamilyMemberResponseDTO> BuildFamilyTree(List<FamilyMemberResponseDTO> flatList)
    {
        var lookup = flatList.ToLookup(fm => fm.ParentFamilyMemberId);

        void AttachChildren(FamilyMemberResponseDTO node)
        {
            node.Children = lookup[node.Id].ToList();
            foreach (var child in node.Children)
                AttachChildren(child);
        }

        var roots = lookup[null].ToList();
        foreach (var root in roots)
            AttachChildren(root);

        return roots;
    }
}
