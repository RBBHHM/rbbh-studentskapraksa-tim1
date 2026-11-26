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
public class CodeListService(ConnectedPartiesDbContext dbContext, IHttpContextAccessor? accessor = null) : ICodeListService
{
    private readonly ConnectedPartiesDbContext _dbContext = dbContext;

    // ─── READ ─────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<object>> GetCategoriesAsync()
    {
        var categories = await _dbContext.CodeListDefinitions.AsNoTracking()
            .OrderBy(item => item.Name).Select(item => item.Name).ToListAsync();

        return Result<object>.Success(new { categories });
    }

    /// <inheritdoc/>
    public async Task<Result<List<CodeListDropdownDTO>>> GetDropdownByKategorija(string kategorija)
    {
        if (string.IsNullOrWhiteSpace(kategorija))
            return Result<List<CodeListDropdownDTO>>.ValidationError(BackendMessages.Message("validation.codelist.category.required"));

        var items = await _dbContext.CodeLists
            .Where(x => x.Definition!.Name == kategorija && x.Aktivan && !_dbContext.CodeListDefinitions.Any(definition => definition.Name == x.Definition!.Name && !definition.IsActive))
            .OrderBy(x => x.Naziv).ThenBy(x => x.ID)
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
            .Where(x => x.Definition!.Name == kategorija)
            .OrderBy(x => x.Naziv).ThenBy(x => x.ID)
            .Include(row => row.Definition)
            .ToListAsync();

        return Result<List<CodeListResponseDTO>>.Success(items.Adapt<List<CodeListResponseDTO>>());
    }

    /// <inheritdoc/>
    public async Task<Result<CodeListResponseDTO>> GetByID(int id)
    {
        if (id < 1)
            return Result<CodeListResponseDTO>.ValidationError(BackendMessages.Message("validation.identifier.invalid"));

        var item = await _dbContext.CodeLists
            .IgnoreQueryFilters()
            .Include(row => row.Definition)
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
        var definition = await _dbContext.CodeListDefinitions.SingleOrDefaultAsync(item => item.Name.ToUpper() == normalizedCategory);
        var code = dto.Kod.Trim().ToUpperInvariant();
        // Inactive values retain their stable key and must be reactivated, not duplicated.
        var postoji = await _dbContext.CodeLists.IgnoreQueryFilters().AnyAsync(x =>
            x.Definition!.Name.Trim().ToUpper() == normalizedCategory && x.Kod.Trim().ToUpper() == code);

        if (postoji)
            return Result<CodeListResponseDTO>.ValidationError(
                BackendMessages.Message("validation.codelist.value.duplicate", dto.Kod, dto.Kategorija), code: "codes.value_duplicate");

        var entitet = new CodeList
        {
            Kategorija        = category,
            Kod               = code,
            Naziv             = dto.Naziv.Trim(),
            Opis              = dto.Opis?.Trim(),
            Aktivan           = dto.Aktivan,
            KreiranDatum      = DateTime.UtcNow,
            KreiraoKorisnik   = korisnik,
        };

        if (definition is not null && !CodebookDefinitions.UsesReferenceFields(definition))
            return Result<CodeListResponseDTO>.ValidationError(BackendMessages.Message("validation.codelist.inactive"), code: "codes.schema_invalid");
        if (definition is { IsActive: false })
            return Result<CodeListResponseDTO>.ValidationError(BackendMessages.Message("validation.codelist.inactive"), code: "codes.value_inactive_category");
        if (definition is null)
        {
            definition = new CodeListDefinition { Name = category, CreatedBy = korisnik };
            _dbContext.CodeListDefinitions.Add(definition);
            CodebookDefinitions.ConfigureReferenceFields(_dbContext, definition);
        }
        definition.ModifiedBy = korisnik;
        entitet.Definition = definition;
        _dbContext.CodeLists.Add(entitet);
        AddAudit("CODELIST_ADD", korisnik, null, Snapshot(entitet));
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
            .Include(row => row.Definition)
            .FirstOrDefaultAsync(x => x.ID == id);

        if (entitet is null)
            return Result<CodeListResponseDTO>.NotFoundError(BackendMessages.Message("validation.codelist.value.missing", id));

        if (entitet.Definition is { } definition && !CodebookDefinitions.UsesReferenceFields(definition))
            return Result<CodeListResponseDTO>.ValidationError(BackendMessages.Message("validation.codelist.inactive"));

        // Updating Aktivan must enforce the same reference guard as Delete.
        if (entitet.Aktivan && !dto.Aktivan && (await IsSystemRequiredAsync(entitet.Kategorija) || await JeVrijednostUUpotrebi(entitet.Kategorija, entitet.Kod)))
            return Result<CodeListResponseDTO>.ValidationError(BackendMessages.Message("validation.codelist.value.in.use"), code: "codes.value_in_use");

        // Ažuriramo dopuštena polja (Kategorija i Kod se ne mijenjaju)
        var before = Snapshot(entitet);
        entitet.Naziv             = dto.Naziv.Trim();
        entitet.Opis              = dto.Opis?.Trim();
        entitet.Aktivan           = dto.Aktivan;
        entitet.IzmijenjenDatum   = DateTime.UtcNow;
        entitet.IzmijenioKorisnik = korisnik;

        AddAudit("CODELIST_UPDATE", korisnik, before, Snapshot(entitet));
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
            .Include(row => row.Definition)
            .FirstOrDefaultAsync(x => x.ID == id);

        if (entitet is null)
            return Result<bool>.NotFoundError(BackendMessages.Message("validation.codelist.value.missing", id));

        // PL-37 — provjera upotrebe
        var uUpotrebi = await IsSystemRequiredAsync(entitet.Kategorija) || await JeVrijednostUUpotrebi(entitet.Kategorija, entitet.Kod);
        if (uUpotrebi)
            return Result<bool>.ValidationError(
                BackendMessages.Message("validation.codelist.value.delete.in.use", entitet.Naziv, entitet.Kod));

        // Soft-delete: ne brišemo fizički, samo deaktiviramo
        var before = Snapshot(entitet);
        entitet.Aktivan           = false;
        entitet.IzmijenjenDatum   = DateTime.UtcNow;
        entitet.IzmijenioKorisnik = korisnik;

        AddAudit("CODELIST_DELETE", korisnik, before, Snapshot(entitet));
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
            .Include(row => row.Definition)
            .Where(item => item.Definition!.Name == normalizedCategory)
            .ToListAsync();

        if (definition is null && values.Count == 0)
            return Result<bool>.NotFoundError(BackendMessages.Message("validation.codelist.definition.missing", normalizedCategory));

        if (definition?.IsSystemRequired == true || await JeKategorijaUUpotrebi(normalizedCategory, values.Select(item => item.Kod)))
            return Result<bool>.ValidationError(
                BackendMessages.Message("validation.codelist.definition.delete.in.use", normalizedCategory));

        object CategoryState() => new
        {
            Definition = definition is null ? null : new { definition.Name, definition.Description, definition.IsActive },
            Values = values.OrderBy(value => value.Kod).Select(Snapshot).ToArray()
        };
        var before = CategoryState();
        var changedAt = DateTime.UtcNow;
        foreach (var value in values)
        {
            value.Aktivan = false;
            value.IzmijenjenDatum = changedAt;
            value.IzmijenioKorisnik = korisnik;
        }

        if (definition is not null)
            { definition.IsActive = false; definition.ModifiedBy = korisnik; }

        AddAudit("CODELIST_DELETE", korisnik, before, CategoryState());
        await _dbContext.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private Task<bool> IsSystemRequiredAsync(string category) =>
        _dbContext.CodeListDefinitions.AnyAsync(definition => definition.Name == category && definition.IsSystemRequired);

    // ─── Privatne metode ──────────────────────────────────────────────────────

    /// <summary>
    /// PL-37: Centralna provjera da li je vrijednost šifarnika u upotrebi.
    /// Proširiti switch izraz kad se dodaju novi entiteti koji referenciraju šifarnik.
    /// </summary>
    private async Task<bool> JeVrijednostUUpotrebi(string kategorija, string kod)
    {
        if (await _dbContext.CodeLists.IgnoreQueryFilters().AnyAsync(row => row.Definition!.Name == LanguageBooks.Packs && row.Localization != null && row.Localization.LanguageCode == kod && row.Localization.ResourceKey != null)) return true;
        return kategorija switch
        {
            "OSNOV_POVEZANOSTI" =>
                await _dbContext.LegalEntities.AsNoTracking().AnyAsync(entity => entity.BasisOfConnection == kod) ||
                await _dbContext.RelatedPersons.AsNoTracking().AnyAsync(person => person.RelationBasis == kod),
            "OSNOV_POSEBNOG_ODNOSA" =>
                await _dbContext.RelatedPersons.AsNoTracking().AnyAsync(person => person.SpecialRelationBasis == kod),
            "VRSTE_LIMITA" =>
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
            "OSNOV_POVEZANOSTI" =>
                await _dbContext.LegalEntities.AsNoTracking().AnyAsync(entity =>
                    entity.BasisOfConnection != null && codes.Contains(entity.BasisOfConnection)) ||
                await _dbContext.RelatedPersons.AsNoTracking().AnyAsync(person =>
                    person.RelationBasis != null && codes.Contains(person.RelationBasis)),
            "OSNOV_POSEBNOG_ODNOSA" =>
                await _dbContext.RelatedPersons.AsNoTracking().AnyAsync(person =>
                    person.SpecialRelationBasis != null && codes.Contains(person.SpecialRelationBasis)),
            "VRSTE_LIMITA" =>
                await _dbContext.Limiti.AsNoTracking().AnyAsync(limit => codes.Contains(limit.TipLimita)),
            _ => false
        };
    }

    private static object Snapshot(CodeList value) => new
    {
        value.Kategorija, value.Kod, value.Naziv, value.Opis, value.Aktivan
    };

    private void AddAudit(string action, string actor, object? before, object after) =>
        _dbContext.AuditLogs.Add(Helpers.BusinessAudit.Create("CodeList",
            action, actor, accessor, before, after));

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
