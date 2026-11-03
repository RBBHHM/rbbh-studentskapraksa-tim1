using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Sifarnici;
using RBBH.ConnectedParties.DL.Entities.Sifarnici;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions.Validations;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>
/// PL-36 + PL-37: Implementacija CRUD operacija nad šifarnikom s provjerom upotrebe pri brisanju.
/// </summary>
public class CodeListService(ConnectedPartiesDbContext dbContext) : ICodeListService
{
    private readonly ConnectedPartiesDbContext _dbContext = dbContext;

    // ─── READ ─────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<object>> GetCategoriesAsync()
    {
        var valueCategories = await _dbContext.CodeLists
            // Category navigation is administrative data too. Keep categories
            // visible even when every value in one category is inactive.
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(x => x.Kategorija)
            .Distinct()
            .ToListAsync();
        var definitions = await _dbContext.CodeListDefinitions.AsNoTracking()
            .Where(item => item.IsActive).Select(item => item.Name).ToListAsync();
        var categories = valueCategories.Concat(definitions).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(k => k).ToList();

        return Result<object>.Success(new { categories });
    }

    /// <inheritdoc/>
    public async Task<Result<List<CodeListDropdownDTO>>> GetDropdownByKategorija(string kategorija)
    {
        if (string.IsNullOrWhiteSpace(kategorija))
            return Result<List<CodeListDropdownDTO>>.ValidationError(BackendMessages.Message("validation.codelist.category.required"));

        var items = await _dbContext.CodeLists
            .Where(x => x.Kategorija == kategorija && x.Aktivan && (x.Definition == null || x.Definition.IsActive))
            .OrderBy(x => x.RedoslijedPrikaza ?? int.MaxValue)
            .ThenBy(x => x.Naziv)
            .ProjectToType<CodeListDropdownDTO>()
            .ToListAsync();

        return Result<List<CodeListDropdownDTO>>.Success(items);
    }

    /// <inheritdoc/>
    public async Task<Result<List<CodeListResponseDTO>>> GetAllByKategorija(string kategorija)
    {
        if (string.IsNullOrWhiteSpace(kategorija))
            return Result<List<CodeListResponseDTO>>.ValidationError(BackendMessages.Message("validation.codelist.category.required"));

        var items = await _dbContext.CodeLists
            // Administrators must be able to see the complete history, including
            // soft-deactivated values. Dropdowns intentionally use the filtered query above.
            .IgnoreQueryFilters()
            .Where(x => x.Kategorija == kategorija)
            .OrderBy(x => x.RedoslijedPrikaza ?? int.MaxValue)
            .ThenBy(x => x.Naziv)
            .ProjectToType<CodeListResponseDTO>()
            .ToListAsync();

        return Result<List<CodeListResponseDTO>>.Success(items);
    }

    /// <inheritdoc/>
    public async Task<Result<CodeListResponseDTO>> GetByID(int id)
    {
        if (id < 1)
            return Result<CodeListResponseDTO>.ValidationError(BackendMessages.Message("validation.identifier.invalid"));

        var item = await _dbContext.CodeLists
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.ID == id);

        if (item is null)
            return Result<CodeListResponseDTO>.NotFoundError(BackendMessages.Message("validation.codelist.value.missing", id));

        return Result<CodeListResponseDTO>.Success(item.Adapt<CodeListResponseDTO>());
    }

    // ─── CREATE ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<CodeListResponseDTO>> Create(CreateCodeListDTO dto, string korisnik)
    {
        var validacija = ValidateCreateDTO(dto);
        if (validacija is not null) return validacija;

        // Provjera duplikata — isti Kod u istoj Kategoriji
        var category = dto.Kategorija.Trim();
        var normalizedCategory = category.ToUpperInvariant();
        var code = dto.Kod.Trim().ToUpperInvariant();
        // Inactive values retain their stable key and must be reactivated, not duplicated.
        var postoji = await _dbContext.CodeLists.IgnoreQueryFilters().AnyAsync(x =>
            x.Kategorija.Trim().ToUpper() == normalizedCategory && x.Kod.Trim().ToUpper() == code);

        if (postoji)
            return Result<CodeListResponseDTO>.ValidationError(
                BackendMessages.Message("validation.codelist.value.duplicate", dto.Kod, dto.Kategorija));

        var entitet = new CodeList
        {
            Kategorija        = category,
            Kod               = code,
            Naziv             = dto.Naziv.Trim(),
            Opis              = dto.Opis?.Trim(),
            RedoslijedPrikaza = dto.RedoslijedPrikaza,
            Aktivan           = dto.Aktivan,
            KreiranDatum      = DateTime.UtcNow,
            KreiraoKorisnik   = korisnik,
        };

        var definition = await _dbContext.CodeListDefinitions.SingleOrDefaultAsync(item => item.Name == category);
        if (definition is not null && !definition.IsActive)
            return Result<CodeListResponseDTO>.ValidationError(BackendMessages.Message("validation.codelist.inactive"));
        if (definition is null)
        {
            definition = new CodeListDefinition { Name = category, CreatedBy = korisnik };
            _dbContext.CodeListDefinitions.Add(definition);
        }
        entitet.DefinitionId = definition.Id;
        definition.ModifiedBy = korisnik;
        _dbContext.CodeLists.Add(entitet);
        AddAudit(entitet.Kategorija, entitet.Kod, entitet.IzmijenjenDatum is null ? "CODELIST_ADD" : "CODELIST_UPDATE", korisnik);
        await _dbContext.SaveChangesAsync();

        return Result<CodeListResponseDTO>.Success(entitet.Adapt<CodeListResponseDTO>());
    }

    // ─── UPDATE ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<CodeListResponseDTO>> Update(int id, UpdateCodeListDTO dto, string korisnik)
    {
        if (id < 1)
            return Result<CodeListResponseDTO>.ValidationError(BackendMessages.Message("validation.identifier.invalid"));

        if (string.IsNullOrWhiteSpace(dto.Naziv))
            return Result<CodeListResponseDTO>.ValidationError(BackendMessages.Message("validation.codelist.name.required"));

        if (dto.Naziv.Length > 200 || dto.Opis?.Length > 500)
            return Result<CodeListResponseDTO>.ValidationError(BackendMessages.Message("validation.codelist.value.length"));

        // Moramo pratiti entitet da bismo ga mogli ažurirati
        var entitet = await _dbContext.CodeLists
            .IgnoreQueryFilters()
            .AsTracking()
            .FirstOrDefaultAsync(x => x.ID == id);

        if (entitet is null)
            return Result<CodeListResponseDTO>.NotFoundError(BackendMessages.Message("validation.codelist.value.missing", id));

        // Updating Aktivan must enforce the same reference guard as Delete.
        if (entitet.Aktivan && !dto.Aktivan && (entitet.IsSystemRequired || await JeVrijednostUUpotrebi(entitet.Kategorija, entitet.Kod)))
            return Result<CodeListResponseDTO>.ValidationError(BackendMessages.Message("validation.codelist.value.in.use"));

        // Ažuriramo dopuštena polja (Kategorija i Kod se ne mijenjaju)
        entitet.Naziv             = dto.Naziv.Trim();
        entitet.Opis              = dto.Opis?.Trim();
        entitet.RedoslijedPrikaza = dto.RedoslijedPrikaza;
        entitet.Aktivan           = dto.Aktivan;
        entitet.IzmijenjenDatum   = DateTime.UtcNow;
        entitet.IzmijenioKorisnik = korisnik;

        AddAudit(entitet.Kategorija, entitet.Kod, entitet.IzmijenjenDatum is null ? "CODELIST_ADD" : "CODELIST_UPDATE", korisnik);
        await _dbContext.SaveChangesAsync();

        return Result<CodeListResponseDTO>.Success(entitet.Adapt<CodeListResponseDTO>());
    }

    // ─── DELETE (PL-37) ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// PL-37: Provjera da li je vrijednost u upotrebi se radi PRIJE brisanja.
    /// Ako jeste → vraća ValidationError s upozorenjem (ne briše).
    /// Ako nije  → soft-delete: Aktivan = false, bilježi korisnika i datum.
    /// </remarks>
    public async Task<Result<bool>> Delete(int id, string korisnik)
    {
        if (id < 1)
            return Result<bool>.ValidationError(BackendMessages.Message("validation.identifier.invalid"));

        var entitet = await _dbContext.CodeLists
            .IgnoreQueryFilters()
            .AsTracking()
            .FirstOrDefaultAsync(x => x.ID == id);

        if (entitet is null)
            return Result<bool>.NotFoundError(BackendMessages.Message("validation.codelist.value.missing", id));

        // PL-37 — provjera upotrebe
        var uUpotrebi = entitet.IsSystemRequired || await JeVrijednostUUpotrebi(entitet.Kategorija, entitet.Kod);
        if (uUpotrebi)
            return Result<bool>.ValidationError(
                BackendMessages.Message("validation.codelist.value.delete.in.use", entitet.Naziv, entitet.Kod));

        // Soft-delete: ne brišemo fizički, samo deaktiviramo
        entitet.Aktivan           = false;
        entitet.IzmijenjenDatum   = DateTime.UtcNow;
        entitet.IzmijenioKorisnik = korisnik;

        AddAudit(entitet.Kategorija, entitet.Kod, "CODELIST_DELETE", korisnik);
        await _dbContext.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> DeleteCategory(string kategorija, string korisnik)
    {
        var normalizedCategory = kategorija?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCategory))
            return Result<bool>.ValidationError(BackendMessages.Message("validation.codelist.definition.required"));

        var definition = await _dbContext.CodeListDefinitions
            .AsTracking()
            .FirstOrDefaultAsync(item => item.Name == normalizedCategory);
        var values = await _dbContext.CodeLists
            .IgnoreQueryFilters()
            .AsTracking()
            .Where(item => item.Kategorija == normalizedCategory)
            .ToListAsync();

        if (definition is null && values.Count == 0)
            return Result<bool>.NotFoundError(BackendMessages.Message("validation.codelist.definition.missing", normalizedCategory));

        if (definition?.IsSystemRequired == true || values.Any(item => item.IsSystemRequired) || await JeKategorijaUUpotrebi(normalizedCategory, values.Select(item => item.Kod)))
            return Result<bool>.ValidationError(
                BackendMessages.Message("validation.codelist.definition.delete.in.use", normalizedCategory));

        var changedAt = DateTime.UtcNow;
        foreach (var value in values)
        {
            value.Aktivan = false;
            value.IzmijenjenDatum = changedAt;
            value.IzmijenioKorisnik = korisnik;
        }

        if (definition is not null)
            { definition.IsActive = false; definition.ModifiedBy = korisnik; }

        AddAudit(normalizedCategory, null, "CODELIST_DELETE", korisnik);
        await _dbContext.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    // ─── Privatne metode ──────────────────────────────────────────────────────

    /// <summary>
    /// PL-37: Centralna provjera da li je vrijednost šifarnika u upotrebi.
    /// Proširiti switch izraz kad se dodaju novi entiteti koji referenciraju šifarnik.
    /// </summary>
    private async Task<bool> JeVrijednostUUpotrebi(string kategorija, string kod)
    {
        if (await _dbContext.CodeLists.IgnoreQueryFilters().AnyAsync(row => row.LanguageCode == kod && row.ResourceKey != null)) return true;
        return kategorija switch
        {
            "OsnovPovezanosti" =>
                await _dbContext.LegalEntities.AsNoTracking().AnyAsync(entity => entity.BasisOfConnection == kod) ||
                await _dbContext.RelatedPersons.AsNoTracking().AnyAsync(person => person.RelationBasis == kod),
            "OsnovPosebnogOdnosa" =>
                await _dbContext.RelatedPersons.AsNoTracking().AnyAsync(person => person.SpecialRelationBasis == kod),
            "VrstaLimita" =>
                await _dbContext.Limiti.AsNoTracking().AnyAsync(limit => limit.TipLimita == kod),
            _ => false
        };
    }

    /// <summary>
    /// Provjerava cijelu kategoriju skupnim upitom, bez po jednog upita za svaku vrijednost.
    /// </summary>
    private async Task<bool> JeKategorijaUUpotrebi(string kategorija, IEnumerable<string> kodovi)
    {
        var codes = kodovi.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (codes.Count == 0) return false;

        return kategorija switch
        {
            "OsnovPovezanosti" =>
                await _dbContext.LegalEntities.AsNoTracking().AnyAsync(entity =>
                    entity.BasisOfConnection != null && codes.Contains(entity.BasisOfConnection)) ||
                await _dbContext.RelatedPersons.AsNoTracking().AnyAsync(person =>
                    person.RelationBasis != null && codes.Contains(person.RelationBasis)),
            "OsnovPosebnogOdnosa" =>
                await _dbContext.RelatedPersons.AsNoTracking().AnyAsync(person =>
                    person.SpecialRelationBasis != null && codes.Contains(person.SpecialRelationBasis)),
            "VrstaLimita" =>
                await _dbContext.Limiti.AsNoTracking().AnyAsync(limit => codes.Contains(limit.TipLimita)),
            _ => false
        };
    }

    private void AddAudit(string category, string? code, string action, string actor) => _dbContext.AuditLogs.Add(new()
    {
        TableName = "CodeList", RecordId = category, Action = action, UserId = actor, Username = actor,
        Timestamp = DateTime.UtcNow, NewValues = System.Text.Json.JsonSerializer.Serialize(new { category, code })
    });

    private static Result<CodeListResponseDTO>? ValidateCreateDTO(CreateCodeListDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Kategorija))
            return Result<CodeListResponseDTO>.ValidationError(BackendMessages.Message("validation.codelist.category.required"));

        if (string.IsNullOrWhiteSpace(dto.Kod))
            return Result<CodeListResponseDTO>.ValidationError(BackendMessages.Message("validation.codelist.code.required"));

        if (string.IsNullOrWhiteSpace(dto.Naziv))
            return Result<CodeListResponseDTO>.ValidationError(BackendMessages.Message("validation.codelist.name.required"));

        if (dto.Kategorija.Length > 100 || dto.Kod.Length > 50 || dto.Naziv.Length > 200 || dto.Opis?.Length > 500)
            return Result<CodeListResponseDTO>.ValidationError(
                BackendMessages.Message("validation.codelist.value.bounds"));

        return null; // Validno
    }
}
