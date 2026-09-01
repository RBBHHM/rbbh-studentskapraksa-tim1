// File: src/RBBH.ConnectedParties/BL/Services/LegalEntityService.cs

using System.Globalization;
using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO;
using RBBH.ConnectedParties.DL.DTO.LegalEntity;
using RBBH.ConnectedParties.DL.Entities.LegalEntity;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions;
using RBBH.ConnectedParties.Helpers.Validators;
using RBBH.ConnectedParties.Helpers.Excel;
using Microsoft.EntityFrameworkCore;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>
/// Implementacija servisa za upravljanje registrom povezanih pravnih lica.
/// </summary>
public class LegalEntityService : ILegalEntityService
{
    private static readonly ExcelImportSchema.Column[] ImportColumns =
    [
        new("Naziv / Name", "Naziv", "Name"),
        new("Tip / Type", "Tip", "Type", "Rezidentnost", "Residency"),
        new("Porezni broj / Tax number", "Porezni broj", "Tax number"),
        new("FBA ID", "FBA ID"),
        new("GCC broj / GCC number", "GCC broj", "GCC number"),
        new("GCC naziv / GCC name", "GCC naziv", "GCC name"),
        new("Matični broj / Registration number", "Matbroj", "Matični broj", "Registration number"),
        new("Osnov povezanosti / Connection basis", "Osnov povezanosti", "Connection basis"),
        new("Opis povezanosti / Connection description", "Opis povezanosti", "Connection description"),
        new("Povezano lice s Bankom / Related to the Bank", "Pov. lice sa Bankom", "Povezano lice s Bankom", "Related to the Bank"),
        new("Datum od / Date from", "Datum od", "Date from"),
        new("Datum do / Date to", "Datum do", "Date to")
    ];
    private readonly ConnectedPartiesDbContext _context;
    private readonly ILogger<LegalEntityService> _logger;

    public LegalEntityService(ConnectedPartiesDbContext context, ILogger<LegalEntityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<LegalEntityListDTO> GetAllAsync(int page, int pageSize, string? search)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = _context.LegalEntities.AsNoTracking().Where(e => e.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = _context.Database.IsRelational()
                ? query.Where(e => EF.Functions.Like(e.Name, $"%{term}%")
                    || (e.TaxNumber != null && e.TaxNumber.Contains(term))
                    || (e.MaticniBroj != null && e.MaticniBroj.Contains(term))
                    || (e.FbaId != null && EF.Functions.Like(e.FbaId, $"%{term}%")))
                : query.Where(e => e.Name.ToLower().Contains(term.ToLower())
                    || (e.TaxNumber != null && e.TaxNumber.Contains(term))
                    || (e.MaticniBroj != null && e.MaticniBroj.Contains(term))
                    || (e.FbaId != null && e.FbaId.ToLower().Contains(term.ToLower())));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => MapToDto(e))
            .ToListAsync();

        return new LegalEntityListDTO
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<LegalEntityDTO>> GetAllForExportAsync() =>
        await _context.LegalEntities
            .AsNoTracking()
            .Where(entity => entity.IsActive)
            .OrderBy(entity => entity.Name)
            .Select(entity => MapToDto(entity))
            .ToListAsync();

    /// <inheritdoc />
    public async Task<LegalEntityDTO?> GetByIdAsync(Guid id)
    {
        var entity = await _context.LegalEntities
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

        return entity is null ? null : MapToDto(entity);
    }

    /// <inheritdoc />
    public async Task<LegalEntityDTO> CreateAsync(CreateLegalEntityDTO dto, string createdBy)
    {
        ValidateCreateDto(dto);
        await ValidateUniqueIdentifierAsync(dto.IsResident, dto.TaxNumber, dto.FbaId);

        if (dto.ConnectedWithBank is null)
            throw new ValidationException("connectedWithBank", "Polje 'Povezano lice sa Bankom' je obavezno.");

        var entity = new LegalEntity
        {
            IsResident = dto.IsResident,
            TaxNumber = dto.IsResident ? dto.TaxNumber?.Trim() : null,
            MaticniBroj = dto.MaticniBroj?.Trim(),
            FbaId = !dto.IsResident ? dto.FbaId?.Trim() : null,
            Name = dto.Name.Trim(),
            Matbroj = string.IsNullOrWhiteSpace(dto.Matbroj) ? null : dto.Matbroj.Trim(),
            GccNumber = dto.GccNumber?.Trim(),
            GccName = dto.GccName?.Trim(),
            BasisOfConnection = dto.BasisOfConnection.Trim(),
            ConnectionDescription = dto.ConnectionDescription?.Trim(),
            ConnectedWithBank = dto.ConnectedWithBank,
            DateFrom = dto.DateFrom,
            DateTo = dto.DateTo,
            Status = "Draft",
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.LegalEntities.Add(entity);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Pravno lice kreirano. Id={Id}; Name={Name}; CreatedBy={CreatedBy}",
            entity.Id, entity.Name, createdBy);

        return MapToDto(entity);
    }

    /// <inheritdoc />
    public async Task<LegalEntityDTO> UpdateAsync(Guid id, UpdateLegalEntityDTO dto, string modifiedBy)
    {
        var entity = await _context.LegalEntities
            .AsTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity is null)
            throw new ValidationException("id", $"Pravno lice s ID-om {id} nije pronađeno.");

        ValidateUpdateDto(dto);
        await ValidateUniqueIdentifierAsync(dto.IsResident, dto.TaxNumber, dto.FbaId, id);

        if (dto.ConnectedWithBank is null)
            throw new ValidationException("connectedWithBank", "Polje 'Povezano lice sa Bankom' je obavezno.");

        entity.IsResident = dto.IsResident;
        entity.TaxNumber = dto.IsResident ? dto.TaxNumber?.Trim() : null;
        entity.FbaId = dto.IsResident ? null : dto.FbaId?.Trim();
        entity.Name = dto.Name.Trim();
        entity.MaticniBroj = string.IsNullOrWhiteSpace(dto.MaticniBroj) ? null : dto.MaticniBroj.Trim();
        entity.Matbroj = string.IsNullOrWhiteSpace(dto.Matbroj) ? null : dto.Matbroj.Trim();
        entity.GccNumber = dto.GccNumber?.Trim();
        entity.GccName = dto.GccName?.Trim();
        entity.BasisOfConnection = dto.BasisOfConnection.Trim();
        entity.ConnectionDescription = dto.ConnectionDescription?.Trim();
        entity.ConnectedWithBank = dto.ConnectedWithBank;
        entity.DateFrom = dto.DateFrom;
        entity.DateTo = dto.DateTo;
        if (!string.IsNullOrWhiteSpace(dto.Status)) entity.Status = dto.Status.Trim();
        entity.ModifiedBy = modifiedBy;
        entity.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Pravno lice ažurirano. Id={Id}; ModifiedBy={ModifiedBy}",
            entity.Id, modifiedBy);

        return MapToDto(entity);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, string deletedBy)
    {
        var entity = await _context.LegalEntities
            .AsTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity is null)
            throw new ValidationException("id", $"Pravno lice s ID-om {id} nije pronađeno.");

        entity.IsActive = false;
        entity.ModifiedBy = deletedBy;
        entity.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Pravno lice deaktivirano (soft delete). Id={Id}; DeletedBy={DeletedBy}",
            id, deletedBy);
    }

    /// <inheritdoc />
    public async Task<LegalEntityDTO> VerifyAsync(Guid id, string verifiedBy)
    {
        var entity = await _context.LegalEntities
            .AsTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

        if (entity is null)
            throw new ValidationException("id", $"Pravno lice s ID-om {id} nije pronađeno.");

        if (string.Equals(entity.Status, "Verified", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("status", "Pravno lice je već verificirano.");

        entity.Status = "Verified";
        entity.VerifiedBy = verifiedBy;
        entity.VerifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = verifiedBy;
        entity.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Pravno lice verificirano. Id={Id}; VerifiedBy={VerifiedBy}",
            entity.Id, verifiedBy);

        return MapToDto(entity);
    }
        /// <inheritdoc />
    public async Task<List<LegalEntityLookupDTO>> SearchForLimitsAsync(string? search)
    {
        var query = _context.LegalEntities
            .AsNoTracking()
            .Where(e => e.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = _context.Database.IsRelational()
                ? query.Where(e => EF.Functions.Like(e.Name, $"%{term}%")
                    || (e.MaticniBroj != null && e.MaticniBroj.Contains(term)))
                : query.Where(e => e.Name.ToLower().Contains(term.ToLower())
                    || (e.MaticniBroj != null && e.MaticniBroj.Contains(term)));
        }

        return await query
            .OrderBy(e => e.Name)
            .Take(20)
            .Select(e => new LegalEntityLookupDTO
            {
                Id = e.Id,
                Name = e.Name,
                MaticniBroj = e.MaticniBroj,
                TaxNumber = e.TaxNumber
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<LegalEntityLimitFormDataDTO?> GetLimitFormDataAsync(Guid legalEntityId)
    {
        return await _context.LegalEntities
            .AsNoTracking()
            .Where(e => e.Id == legalEntityId && e.IsActive)
            .Select(e => new LegalEntityLimitFormDataDTO
            {
                Id = e.Id,
                IsResident = e.IsResident,
                FbaId = e.FbaId,
                TaxNumber = e.TaxNumber,
                MaticniBroj = e.MaticniBroj,
                Name = e.Name,
                GccNumber = e.GccNumber,
                GccName = e.GccName
            })
            .FirstOrDefaultAsync();
    }

    // ── Privatni pomoćni metodi ───────────────────────────────────────────────

    private static void ValidateCreateDto(CreateLegalEntityDTO dto)
    {
        if (dto.IsResident)
        {
            if (string.IsNullOrWhiteSpace(dto.TaxNumber))
                throw new ValidationException("taxNumber", "Porezni broj je obavezan za rezidentno pravno lice.");

            TaxNumberValidator.ValidateTaxNumber(dto.TaxNumber);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.FbaId))
                throw new ValidationException("fbaId", "FBA ID je obavezan za nerezidentno pravno lice.");

            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.FbaId.Trim(), @"^\d{1,10}$"))
                throw new ValidationException("fbaId", "FBA ID mora sadržavati maksimalno 10 cifara.");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ValidationException("name", "Naziv je obavezan.");

        if (string.IsNullOrWhiteSpace(dto.BasisOfConnection))
            throw new ValidationException("basisOfConnection", "Osnov povezanosti je obavezan.");

        ValidateRequiredBusinessFields(dto.GccNumber, dto.GccName, dto.ConnectionDescription, dto.DateFrom);

        if (dto.DateFrom.HasValue && dto.DateTo.HasValue && dto.DateTo < dto.DateFrom)
            throw new ValidationException("dateTo", "Datum do mora biti nakon datuma od.");
    }

    private static void ValidateUpdateDto(UpdateLegalEntityDTO dto)
    {
        if (dto.IsResident)
        {
            if (string.IsNullOrWhiteSpace(dto.TaxNumber))
                throw new ValidationException("taxNumber", "Porezni broj je obavezan za rezidentno pravno lice.");
            TaxNumberValidator.ValidateTaxNumber(dto.TaxNumber);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.FbaId))
                throw new ValidationException("fbaId", "FBA ID je obavezan za nerezidentno pravno lice.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.FbaId.Trim(), @"^\d{1,10}$"))
                throw new ValidationException("fbaId", "FBA ID mora sadržavati maksimalno 10 cifara.");
        }
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ValidationException("name", "Naziv je obavezan.");
        if (string.IsNullOrWhiteSpace(dto.BasisOfConnection))
            throw new ValidationException("basisOfConnection", "Osnov povezanosti je obavezan.");
        ValidateRequiredBusinessFields(dto.GccNumber, dto.GccName, dto.ConnectionDescription, dto.DateFrom);
        if (dto.DateFrom.HasValue && dto.DateTo.HasValue && dto.DateTo < dto.DateFrom)
            throw new ValidationException("dateTo", "Datum do mora biti nakon datuma od.");
    }

    private static void ValidateRequiredBusinessFields(string? gccNumber, string? gccName,
        string? connectionDescription, DateTime? dateFrom)
    {
        if (string.IsNullOrWhiteSpace(gccNumber) || !gccNumber.All(char.IsDigit))
            throw new ValidationException("gccNumber", "GCC broj je obavezan i mora sadržavati samo cifre.");
        if (string.IsNullOrWhiteSpace(gccName))
            throw new ValidationException("gccName", "GCC naziv je obavezan.");
        if (string.IsNullOrWhiteSpace(connectionDescription))
            throw new ValidationException("connectionDescription", "Opis osnova povezanosti je obavezan.");
        if (!dateFrom.HasValue)
            throw new ValidationException("dateFrom", "Datum početka povezanosti je obavezan.");
    }

    private async Task ValidateUniqueIdentifierAsync(bool isResident, string? taxNumber, string? fbaId, Guid? excludedId = null)
    {
        if (isResident && !string.IsNullOrWhiteSpace(taxNumber))
        {
            var exists = await _context.LegalEntities
                .AsNoTracking()
                .AnyAsync(e => e.TaxNumber == taxNumber.Trim() && (!excludedId.HasValue || e.Id != excludedId));

            if (exists)
                throw new ValidationException("taxNumber", $"Pravno lice s poreznim brojem {taxNumber} već postoji.");
        }
        else if (!isResident && !string.IsNullOrWhiteSpace(fbaId))
        {
            var exists = await _context.LegalEntities
                .AsNoTracking()
                .AnyAsync(e => e.FbaId == fbaId.Trim() && (!excludedId.HasValue || e.Id != excludedId));

            if (exists)
                throw new ValidationException("fbaId", $"Pravno lice s FBA ID-om {fbaId} već postoji.");
        }
    }

    /// <inheritdoc />
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
        var usedTaxNumbers = (await _context.LegalEntities.AsNoTracking()
            .Where(entity => entity.TaxNumber != null)
            .Select(entity => entity.TaxNumber!)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var usedFbaIds = (await _context.LegalEntities.AsNoTracking()
            .Where(entity => entity.FbaId != null)
            .Select(entity => entity.FbaId!)
            .ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (int row = 2; row <= lastRow; row++)
        {
            try
            {
                var name           = ws.Cell(row, 1).GetString().Trim();
                var tipStr         = ws.Cell(row, 2).GetString().Trim();
                var taxNumber      = ws.Cell(row, 3).GetString().Trim();
                var fbaId          = ws.Cell(row, 4).GetString().Trim();
                var gccNumber      = ws.Cell(row, 5).GetString().Trim();
                var gccName        = ws.Cell(row, 6).GetString().Trim();
                var matbroj        = ws.Cell(row, 7).GetString().Trim();
                var basisOfConn    = ws.Cell(row, 8).GetString().Trim();
                var connDesc       = ws.Cell(row, 9).GetString().Trim();
                var connBankStr    = ws.Cell(row, 10).GetString().Trim();
                var dateFromStr    = ws.Cell(row, 11).GetString().Trim();
                var dateToStr      = ws.Cell(row, 12).GetString().Trim();

                if (string.IsNullOrWhiteSpace(name))
                    continue; // prazan red — preskoči bez greške

                if (string.IsNullOrWhiteSpace(basisOfConn))
                    throw new InvalidOperationException("Osnov povezanosti je obavezan.");

                var isResident = tipStr.Equals("Rezident", StringComparison.OrdinalIgnoreCase) ||
                    tipStr.Equals("Resident", StringComparison.OrdinalIgnoreCase);
                var isNonResident = tipStr.Equals("Nerezident", StringComparison.OrdinalIgnoreCase) ||
                    tipStr.Equals("Non-resident", StringComparison.OrdinalIgnoreCase) ||
                    tipStr.Equals("Nonresident", StringComparison.OrdinalIgnoreCase);
                if (!isResident && !isNonResident)
                    throw new InvalidOperationException("Tip mora biti 'Rezident/Resident' ili 'Nerezident/Non-resident'.");

                bool? connectedWithBank = connBankStr.Equals("DA", StringComparison.OrdinalIgnoreCase) ? true
                    : connBankStr.Equals("YES", StringComparison.OrdinalIgnoreCase) ? true
                    : connBankStr.Equals("NE", StringComparison.OrdinalIgnoreCase) ? false
                    : connBankStr.Equals("NO", StringComparison.OrdinalIgnoreCase) ? false
                    : null;

                if (connectedWithBank is null)
                    throw new InvalidOperationException("Kolona 'Povezano lice s Bankom' mora sadržavati DA/NE ili YES/NO.");

                DateTime? dateFrom = ParseOptionalDate(dateFromStr, "Datum od");
                DateTime? dateTo   = ParseOptionalDate(dateToStr, "Datum do");

                var dto = new CreateLegalEntityDTO
                {
                    IsResident         = isResident,
                    TaxNumber          = isResident && !string.IsNullOrWhiteSpace(taxNumber) ? taxNumber : null,
                    FbaId              = !isResident && !string.IsNullOrWhiteSpace(fbaId) ? fbaId : null,
                    Name               = name,
                    GccNumber          = string.IsNullOrWhiteSpace(gccNumber) ? null : gccNumber,
                    GccName            = string.IsNullOrWhiteSpace(gccName) ? null : gccName,
                    Matbroj            = string.IsNullOrWhiteSpace(matbroj) ? null : matbroj,
                    BasisOfConnection  = basisOfConn,
                    ConnectionDescription = string.IsNullOrWhiteSpace(connDesc) ? null : connDesc,
                    ConnectedWithBank  = connectedWithBank,
                    DateFrom           = dateFrom,
                    DateTo             = dateTo
                };
                ValidateCreateDto(dto);
                if (dto.ConnectedWithBank is null)
                    throw new InvalidOperationException("Kolona 'Povezano lice s Bankom' mora sadržavati DA/NE ili YES/NO.");
                var identifier = isResident ? taxNumber : fbaId;
                var identifiers = isResident ? usedTaxNumbers : usedFbaIds;
                if (!identifiers.Add(identifier))
                    throw new InvalidOperationException(isResident
                        ? "Pravno lice s ovim poreznim brojem već postoji."
                        : "Pravno lice s ovim FBA ID-em već postoji.");

                var entity = new LegalEntity
                {
                    IsResident = dto.IsResident,
                    TaxNumber = dto.IsResident ? dto.TaxNumber : null,
                    FbaId = dto.IsResident ? null : dto.FbaId,
                    Name = dto.Name,
                    GccNumber = dto.GccNumber,
                    GccName = dto.GccName,
                    Matbroj = dto.Matbroj,
                    BasisOfConnection = dto.BasisOfConnection,
                    ConnectionDescription = dto.ConnectionDescription,
                    ConnectedWithBank = dto.ConnectedWithBank,
                    DateFrom = dto.DateFrom,
                    DateTo = dto.DateTo,
                    Status             = "Draft",
                    IsActive           = true,
                    CreatedAt          = DateTime.UtcNow,
                    CreatedBy          = createdBy
                };

                _context.LegalEntities.Add(entity);
                result.Imported++;
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"Red {row}: {ex.Message}");
            }
        }

        if (result.Imported > 0)
            await _context.SaveChangesAsync();

        return result;
    }

    private static DateTime? ParseOptionalDate(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParse(value, CultureInfo.GetCultureInfo("bs-BA"), DateTimeStyles.None, out var date) ||
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return date;
        throw new InvalidOperationException($"Kolona '{field}' ne sadrži ispravan datum.");
    }

    private static LegalEntityDTO MapToDto(LegalEntity e) => new()
    {
        Id = e.Id,
        IsResident = e.IsResident,
        TaxNumber = e.TaxNumber,
        MaticniBroj = e.MaticniBroj,
        FbaId = e.FbaId,
        Name = e.Name,
        Matbroj = e.Matbroj,
        GccNumber = e.GccNumber,
        GccName = e.GccName,
        BasisOfConnection = e.BasisOfConnection,
        ConnectionDescription = e.ConnectionDescription,
        ConnectedWithBank = e.ConnectedWithBank,
        DateFrom = e.DateFrom,
        DateTo = e.DateTo,
        Status = e.Status,
        CreatedBy = e.CreatedBy,
        CreatedAt = e.CreatedAt,
        ModifiedBy = e.ModifiedBy,
        ModifiedAt = e.ModifiedAt,
        VerifiedBy = e.VerifiedBy,
        VerifiedAt = e.VerifiedAt
    };
}
