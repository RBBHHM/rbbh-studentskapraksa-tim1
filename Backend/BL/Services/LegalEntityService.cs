// File: src/RBBH.ConnectedParties/BL/Services/LegalEntityService.cs

using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO;
using RBBH.ConnectedParties.DL.DTO.LegalEntity;
using RBBH.ConnectedParties.DL.Entities.LegalEntity;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions;
using RBBH.ConnectedParties.Helpers.Validators;
using Microsoft.EntityFrameworkCore;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>
/// Implementacija servisa za upravljanje registrom povezanih pravnih lica.
/// </summary>
public partial class LegalEntityService : ILegalEntityService
{
    // Excel import schema is defined alongside its implementation in the partial type.
    private readonly ConnectedPartiesDbContext _context;
    private readonly ILogger<LegalEntityService> _logger;
    private readonly IHttpContextAccessor? _actor;
    private readonly IBankReferenceSource? _bank;

    public LegalEntityService(ConnectedPartiesDbContext context, ILogger<LegalEntityService> logger, IHttpContextAccessor? actor = null, IBankReferenceSource? bank = null)
    {
        _context = context;
        _logger = logger;
        _actor = actor;
        _bank = bank;
    }

    /// <inheritdoc />
    public async Task<LegalEntityListDTO> GetAllAsync(int page, int pageSize, string? search)
    {
        page = Math.Clamp(page, 1, 1_000_000);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = _context.LegalEntities.AsNoTracking().Where(e => e.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = _context.Database.IsRelational()
                ? query.Where(e => EF.Functions.Like(EF.Functions.Collate(e.Name, "Latin1_General_100_CI_AS"), $"%{term}%")
                    || (e.TaxNumber != null && e.TaxNumber.Contains(term))
                    || (e.MaticniBroj != null && e.MaticniBroj.Contains(term))
                    || (e.FbaId != null && EF.Functions.Like(EF.Functions.Collate(e.FbaId, "Latin1_General_100_CI_AS"), $"%{term}%")))
                : query.Where(e => e.Name.ToLower().Contains(term.ToLower())
                    || (e.TaxNumber != null && e.TaxNumber.Contains(term))
                    || (e.MaticniBroj != null && e.MaticniBroj.Contains(term))
                    || (e.FbaId != null && e.FbaId.ToLower().Contains(term.ToLower())));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.CreatedAt).ThenBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(DtoProjection)
            .ToListAsync();

        return new LegalEntityListDTO
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<LegalEntityDTO>> GetAllForExportAsync()
    {
        var items = await _context.LegalEntities.AsNoTracking().OrderBy(row => row.Name).ThenBy(row => row.Id).Take(5001)
            .Select(DtoProjection).ToListAsync(_actor?.HttpContext?.RequestAborted ?? default);
        if (items.Count > 5000) throw new ValidationException("file", BackendMessages.Message("errors.workbook.export.limit"));
        return items;
    }

    /// <inheritdoc />
    public async Task<LegalEntityDTO?> GetByIdAsync(Guid id)
    {
        return await _context.LegalEntities.AsNoTracking()
            .Where(e => e.Id == id && e.IsActive)
            .Select(DtoProjection)
            .FirstOrDefaultAsync(_actor?.HttpContext?.RequestAborted ?? default);
    }

    /// <inheritdoc />
    public async Task<LegalEntityDTO> CreateAsync(CreateLegalEntityDTO dto, string createdBy)
    {
        ValidateCreateDto(dto);
        (await Helpers.CodebookChoices.Load(_context, _actor?.HttpContext?.RequestAborted ?? default, "OsnovPovezanosti"))
            .Check("OsnovPovezanosti", dto.BasisOfConnection, "basisOfConnection");
        var gcc = await ResolveGcc(dto.GccNumber);
        await ValidateUniqueIdentifierAsync(dto.IsResident, dto.TaxNumber, dto.FbaId);

        if (dto.ConnectedWithBank is null)
            throw new ValidationException("connectedWithBank", BackendMessages.Message("validation.registry.bank.relation.required"));

        var entity = new LegalEntity
        {
            IsResident = dto.IsResident,
            TaxNumber = dto.IsResident ? dto.TaxNumber?.Trim() : null,
            MaticniBroj = dto.MaticniBroj?.Trim(),
            FbaId = !dto.IsResident ? dto.FbaId?.Trim() : null,
            Name = dto.Name.Trim(),
            GccNumber = gcc?.Number,
            GccName = gcc?.Name,
            IsLargeExposure = dto.IsLargeExposure ?? false,
            Matbroj = string.IsNullOrWhiteSpace(dto.Matbroj) ? null : dto.Matbroj.Trim(),
            BasisOfConnection = dto.BasisOfConnection.Trim(),
            ConnectionDescription = dto.ConnectionDescription?.Trim(),
            ConnectedWithBank = dto.ConnectedWithBank,
            DateFrom = dto.DateFrom,
            DateTo = dto.DateTo,
            Status = "Draft",
            CreatorSubject = Helpers.RegistryVerification.CreatorSubject(_actor),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.LegalEntities.Add(entity);
        _context.AuditLogs.Add(Helpers.BusinessAudit.Create("LegalEntity", entity.Id.ToString(), "INSERT", createdBy, _actor, newValues: MapToDto(entity)));
        await _context.SaveChangesAsync(_actor?.HttpContext?.RequestAborted ?? default);

        _logger.LogInformation("Legal entity created.");

        return MapToDto(entity);
    }

    /// <inheritdoc />
    public async Task<LegalEntityDTO> UpdateAsync(Guid id, UpdateLegalEntityDTO dto, string modifiedBy)
    {
        var entity = await _context.LegalEntities
            .AsTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity is null)
            throw new ValidationException("id", BackendMessages.Message("validation.registry.legal.id.missing", id));

        ValidateUpdateDto(dto);
        (await Helpers.CodebookChoices.Load(_context, _actor?.HttpContext?.RequestAborted ?? default, "OsnovPovezanosti"))
            .Check("OsnovPovezanosti", dto.BasisOfConnection, "basisOfConnection");
        // Omitted additive fields preserve previous source-backed values during rollout.
        var gcc = dto.GccNumber is null && entity.GccNumber is not null ? null : await ResolveGcc(dto.GccNumber);
        await ValidateUniqueIdentifierAsync(dto.IsResident, dto.TaxNumber, dto.FbaId, id);

        if (dto.ConnectedWithBank is null)
            throw new ValidationException("connectedWithBank", BackendMessages.Message("validation.registry.bank.relation.required"));

        // Status is owned by verification, even if an old client sends it back.
        if (!string.IsNullOrWhiteSpace(dto.Status) && !string.Equals(dto.Status.Trim(), entity.Status, StringComparison.Ordinal))
            throw new BusinessRuleException(400, "verification.status_owned");

        var before = MapToDto(entity);
        if (dto.GccNumber is not null) { entity.GccNumber = gcc?.Number; entity.GccName = gcc?.Name; }
        if (dto.IsLargeExposure.HasValue) entity.IsLargeExposure = dto.IsLargeExposure.Value;
        entity.IsResident = dto.IsResident;
        entity.TaxNumber = dto.IsResident ? dto.TaxNumber?.Trim() : null;
        entity.FbaId = dto.IsResident ? null : dto.FbaId?.Trim();
        entity.Name = dto.Name.Trim();
        entity.MaticniBroj = string.IsNullOrWhiteSpace(dto.MaticniBroj) ? null : dto.MaticniBroj.Trim();
        entity.Matbroj = string.IsNullOrWhiteSpace(dto.Matbroj) ? null : dto.Matbroj.Trim();
        entity.BasisOfConnection = dto.BasisOfConnection.Trim();
        entity.ConnectionDescription = dto.ConnectionDescription?.Trim();
        entity.ConnectedWithBank = dto.ConnectedWithBank;
        entity.DateFrom = dto.DateFrom;
        entity.DateTo = dto.DateTo;
        entity.ModifiedBy = modifiedBy;
        entity.ModifiedAt = DateTime.UtcNow;

        _context.AuditLogs.Add(Helpers.BusinessAudit.Create("LegalEntity", entity.Id.ToString(), "UPDATE", modifiedBy, _actor, oldValues: before, newValues: MapToDto(entity)));
        await _context.SaveChangesAsync(_actor?.HttpContext?.RequestAborted ?? default);

        _logger.LogInformation("Legal entity updated.");

        return MapToDto(entity);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, string deletedBy)
    {
        var entity = await _context.LegalEntities
            .AsTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity is null)
            throw new ValidationException("id", BackendMessages.Message("validation.registry.legal.id.missing", id));

        entity.IsActive = false;
        entity.ModifiedBy = deletedBy;
        entity.ModifiedAt = DateTime.UtcNow;

        _context.AuditLogs.Add(Helpers.BusinessAudit.Create("LegalEntity", entity.Id.ToString(), "DELETE", deletedBy, _actor,
            oldValues: new { isActive = true }, newValues: new { isActive = false }));
        await _context.SaveChangesAsync(_actor?.HttpContext?.RequestAborted ?? default);

        _logger.LogInformation("Legal entity deactivated.");
    }

    /// <inheritdoc />
    public async Task<LegalEntityDTO> VerifyAsync(Guid id, string verifiedBy)
    {
        var entity = await _context.LegalEntities
            .AsTracking()
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

        if (entity is null)
            throw new ValidationException("id", BackendMessages.Message("validation.registry.legal.id.missing", id));

        if (string.Equals(entity.Status, "Verified", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("status", BackendMessages.Message("validation.registry.legal.verified"));

        Helpers.RegistryVerification.Authorize(entity.CreatorSubject, _actor);
        entity.Status = "Verified";
        entity.VerifiedBy = verifiedBy;
        entity.VerifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = verifiedBy;
        entity.ModifiedAt = DateTime.UtcNow;

        _context.AuditLogs.Add(Helpers.RegistryVerification.Audit("LegalEntity", entity.Id, verifiedBy, _actor));
        await _context.SaveChangesAsync();

        _logger.LogInformation("Legal entity verified.");

        return MapToDto(entity);
    }
        /// <inheritdoc />
    public async Task<List<LegalEntityLookupDTO>> SearchForLimitsAsync(string? search) =>
        (await SearchForLimitsPageAsync(search, 1, 20, _actor?.HttpContext?.RequestAborted ?? default)).Items.ToList();

    public async Task<ReferencePage<LegalEntityLookupDTO>> SearchForLimitsPageAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        if (search?.Length > 200) throw new BusinessRuleException(400, "registry.invalid_search");
        page = Math.Clamp(page, 1, 1_000_000); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.LegalEntities.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(row => row.Name.ToUpper().Contains(term) || row.MaticniBroj != null && row.MaticniBroj.Contains(term) || row.TaxNumber != null && row.TaxNumber.Contains(term));
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(row => row.Name).ThenBy(row => row.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(row => new LegalEntityLookupDTO { Id = row.Id, Name = row.Name, TaxNumber = row.TaxNumber, MaticniBroj = row.MaticniBroj }).ToListAsync(ct);
        return new(items, total, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<LegalEntityLimitFormDataDTO?> GetLimitFormDataAsync(Guid legalEntityId)
    {
        var result = await _context.LegalEntities.AsNoTracking()
            .Where(entity => entity.Id == legalEntityId && entity.IsActive)
            .Select(entity => new LegalEntityLimitFormDataDTO
            {
                Id = entity.Id,
                IsResident = entity.IsResident,
                FbaId = entity.FbaId,
                TaxNumber = entity.TaxNumber,
                MaticniBroj = entity.MaticniBroj,
                Matbroj = entity.Matbroj,
                Name = entity.Name,
            })
            .FirstOrDefaultAsync();

        if (result is null)
            return null;

        // Kapital je jedan globalni zapis (nije po pravnom licu) — čita se odvojeno.
        var capital = await CapitalHistory.AsOf(_context, DateTime.UtcNow.Date, _actor?.HttpContext?.RequestAborted ?? default);
        result.RegulatoryCapital = capital?.RegulatoryCapital;
        result.CoreCapital = capital?.CoreCapital;

        return result;
    }

    // ── Privatni pomoćni metodi ───────────────────────────────────────────────

    private async Task<BankGroup?> ResolveGcc(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            if (_bank?.IsConfigured == true) throw new BusinessRuleException(400, "registry.gcc_required");
            return null;
        }
        if (number.Length > 50 || number.Any(character => character is < '0' or > '9'))
            throw new BusinessRuleException(400, "registry.invalid_gcc");
        if (_bank is null) throw new BusinessRuleException(503, "bank_reference.unavailable");
        var group = await _bank.Group(number, _actor?.HttpContext?.RequestAborted ?? default)
            ?? throw new BusinessRuleException(400, "registry.unknown_gcc");
        if (group.Number != number || string.IsNullOrWhiteSpace(group.Name) || group.Name.Length > 200)
            throw new BusinessRuleException(503, "bank_reference.invalid_record");
        return group;
    }

    private static void ValidateCreateDto(CreateLegalEntityDTO dto)
    {
        ValidateStoredFields(dto.Name, dto.MaticniBroj, dto.Matbroj, dto.BasisOfConnection, dto.ConnectionDescription);
        if (dto.IsResident)
        {
            if (string.IsNullOrWhiteSpace(dto.TaxNumber))
                throw new ValidationException("taxNumber", BackendMessages.Message("validation.registry.tax.resident.required"));

            TaxNumberValidator.ValidateTaxNumber(dto.TaxNumber);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.FbaId))
                throw new ValidationException("fbaId", BackendMessages.Message("validation.registry.fba.nonresident.required"));

            if (dto.FbaId.Trim().Length > 10 || !dto.FbaId.Trim().All(char.IsAsciiDigit))
                throw new ValidationException("fbaId", BackendMessages.Message("validation.registry.fba.digit.limit"));
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ValidationException("name", BackendMessages.Message("validation.registry.name.required"));

        if (string.IsNullOrWhiteSpace(dto.BasisOfConnection))
            throw new ValidationException("basisOfConnection", BackendMessages.Message("validation.registry.connection.required"));

        ValidateRequiredBusinessFields(dto.ConnectionDescription, dto.DateFrom);

        if (dto.DateFrom.HasValue && dto.DateTo.HasValue && dto.DateTo < dto.DateFrom)
            throw new ValidationException("dateTo", BackendMessages.Message("validation.registry.date.order"));
    }

    private static void ValidateUpdateDto(UpdateLegalEntityDTO dto)
    {
        ValidateStoredFields(dto.Name, dto.MaticniBroj, dto.Matbroj, dto.BasisOfConnection, dto.ConnectionDescription);
        if (dto.IsResident)
        {
            if (string.IsNullOrWhiteSpace(dto.TaxNumber))
                throw new ValidationException("taxNumber", BackendMessages.Message("validation.registry.tax.resident.required"));
            TaxNumberValidator.ValidateTaxNumber(dto.TaxNumber);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.FbaId))
                throw new ValidationException("fbaId", BackendMessages.Message("validation.registry.fba.nonresident.required"));
            if (dto.FbaId.Trim().Length > 10 || !dto.FbaId.Trim().All(char.IsAsciiDigit))
                throw new ValidationException("fbaId", BackendMessages.Message("validation.registry.fba.digit.limit"));
        }
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ValidationException("name", BackendMessages.Message("validation.registry.name.required"));
        if (string.IsNullOrWhiteSpace(dto.BasisOfConnection))
            throw new ValidationException("basisOfConnection", BackendMessages.Message("validation.registry.connection.required"));
        ValidateRequiredBusinessFields(dto.ConnectionDescription, dto.DateFrom);
        if (dto.DateFrom.HasValue && dto.DateTo.HasValue && dto.DateTo < dto.DateFrom)
            throw new ValidationException("dateTo", BackendMessages.Message("validation.registry.date.order"));
    }

    private static void ValidateStoredFields(string name, string? maticniBroj, string? matbroj,
        string basis, string? description)
    {
        if (name?.Length > 100)
            throw new ValidationException("name", BackendMessages.Message("validation.registry.name.length"));
        if (basis?.Length > 100)
            throw new ValidationException("basisOfConnection", BackendMessages.Message("validation.registry.connection.length"));
        if (description?.Length > 500)
            throw new ValidationException("connectionDescription", BackendMessages.Message("validation.registry.description.length"));
        ValidateOptionalNumber(maticniBroj, "maticniBroj");
        ValidateOptionalNumber(matbroj, "matbroj");
    }

    private static void ValidateOptionalNumber(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var normalized = value.Trim();
        if (normalized.Length > 50 || !normalized.All(char.IsAsciiDigit))
            throw new ValidationException(field, BackendMessages.Message("validation.registry.registration.length"));
    }

    private static void ValidateRequiredBusinessFields(string? connectionDescription, DateTime? dateFrom)
    {
        if (string.IsNullOrWhiteSpace(connectionDescription))
            throw new ValidationException("connectionDescription", BackendMessages.Message("validation.registry.description.required"));
        if (!dateFrom.HasValue)
            throw new ValidationException("dateFrom", BackendMessages.Message("validation.registry.start.required"));
    }

    private async Task ValidateUniqueIdentifierAsync(bool isResident, string? taxNumber, string? fbaId, Guid? excludedId = null)
    {
        if (isResident && !string.IsNullOrWhiteSpace(taxNumber))
        {
            var exists = await _context.LegalEntities.IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(e => e.TaxNumber == taxNumber.Trim() && (!excludedId.HasValue || e.Id != excludedId));

            if (exists)
                throw new ValidationException("taxNumber", BackendMessages.Message("validation.registry.legal.tax.exists", taxNumber));
        }
        else if (!isResident && !string.IsNullOrWhiteSpace(fbaId))
        {
            var exists = await _context.LegalEntities.IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(e => e.FbaId == fbaId.Trim() && (!excludedId.HasValue || e.Id != excludedId));

            if (exists)
                throw new ValidationException("fbaId", BackendMessages.Message("validation.registry.legal.fba.exists", fbaId));
        }
    }

    // Excel import and validation helpers are implemented in LegalEntityService.Import.cs.

    // Keep a single mapping for both EF's server-side SELECT projection and
    // in-memory mutation/audit responses. Avoid hydrating entire entities for
    // list/export/details; only DTO columns are transferred from SQL Server.
    private static readonly System.Linq.Expressions.Expression<Func<LegalEntity, LegalEntityDTO>> DtoProjection = e => new LegalEntityDTO
    {
        Id = e.Id,
        IsResident = e.IsResident,
        TaxNumber = e.TaxNumber,
        MaticniBroj = e.MaticniBroj,
        FbaId = e.FbaId,
        Name = e.Name,
        GccNumber = e.GccNumber, GccName = e.GccName, IsLargeExposure = e.IsLargeExposure,
        Matbroj = e.Matbroj,
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

    private static readonly Func<LegalEntity, LegalEntityDTO> DtoMapper = DtoProjection.Compile();
    private static LegalEntityDTO MapToDto(LegalEntity entity) => DtoMapper(entity);
}
