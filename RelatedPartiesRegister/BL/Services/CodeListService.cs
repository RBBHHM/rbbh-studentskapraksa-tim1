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
            return Result<List<CodeListDropdownDTO>>.ValidationError("Kategorija ne smije biti prazna.");

        var items = await _dbContext.CodeLists
            .Where(x => x.Kategorija == kategorija && x.Aktivan)
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
            return Result<List<CodeListResponseDTO>>.ValidationError("Kategorija ne smije biti prazna.");

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
            return Result<CodeListResponseDTO>.ValidationError("ID nije validan.");

        var item = await _dbContext.CodeLists
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.ID == id);

        if (item is null)
            return Result<CodeListResponseDTO>.NotFoundError($"Šifarnik s ID={id} nije pronađen.");

        return Result<CodeListResponseDTO>.Success(item.Adapt<CodeListResponseDTO>());
    }

    // ─── CREATE ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<CodeListResponseDTO>> Create(CreateCodeListDTO dto, string korisnik)
    {
        var validacija = ValidateCreateDTO(dto);
        if (validacija is not null) return validacija;

        // Provjera duplikata — isti Kod u istoj Kategoriji
        var postoji = await _dbContext.CodeLists.AnyAsync(x =>
            x.Kategorija == dto.Kategorija && x.Kod == dto.Kod);

        if (postoji)
            return Result<CodeListResponseDTO>.ValidationError(
                $"Vrijednost s kodom '{dto.Kod}' već postoji u kategoriji '{dto.Kategorija}'.");

        var entitet = new CodeList
        {
            Kategorija        = dto.Kategorija.Trim(),
            Kod               = dto.Kod.Trim().ToUpper(),
            Naziv             = dto.Naziv.Trim(),
            Opis              = dto.Opis?.Trim(),
            RedoslijedPrikaza = dto.RedoslijedPrikaza,
            Aktivan           = dto.Aktivan,
            KreiranDatum      = DateTime.UtcNow,
            KreiraoKorisnik   = korisnik,
        };

        _dbContext.CodeLists.Add(entitet);
        await _dbContext.SaveChangesAsync();

        return Result<CodeListResponseDTO>.Success(entitet.Adapt<CodeListResponseDTO>());
    }

    // ─── UPDATE ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<CodeListResponseDTO>> Update(int id, UpdateCodeListDTO dto, string korisnik)
    {
        if (id < 1)
            return Result<CodeListResponseDTO>.ValidationError("ID nije validan.");

        if (string.IsNullOrWhiteSpace(dto.Naziv))
            return Result<CodeListResponseDTO>.ValidationError("Naziv ne smije biti prazan.");

        // Moramo pratiti entitet da bismo ga mogli ažurirati
        var entitet = await _dbContext.CodeLists
            .IgnoreQueryFilters()
            .AsTracking()
            .FirstOrDefaultAsync(x => x.ID == id);

        if (entitet is null)
            return Result<CodeListResponseDTO>.NotFoundError($"Šifarnik s ID={id} nije pronađen.");

        // Ažuriramo dopuštena polja (Kategorija i Kod se ne mijenjaju)
        entitet.Naziv             = dto.Naziv.Trim();
        entitet.Opis              = dto.Opis?.Trim();
        entitet.RedoslijedPrikaza = dto.RedoslijedPrikaza;
        entitet.Aktivan           = dto.Aktivan;
        entitet.IzmijenjenDatum   = DateTime.UtcNow;
        entitet.IzmijenioKorisnik = korisnik;

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
            return Result<bool>.ValidationError("ID nije validan.");

        var entitet = await _dbContext.CodeLists
            .IgnoreQueryFilters()
            .AsTracking()
            .FirstOrDefaultAsync(x => x.ID == id);

        if (entitet is null)
            return Result<bool>.NotFoundError($"Šifarnik s ID={id} nije pronađen.");

        // PL-37 — provjera upotrebe
        var uUpotrebi = await JeVrijednostUUpotrebi(entitet.Kategorija, entitet.Kod);
        if (uUpotrebi)
            return Result<bool>.ValidationError(
                $"Vrijednost '{entitet.Naziv}' ({entitet.Kod}) je trenutno u upotrebi i ne može biti obrisana. " +
                "Prvo je potrebno ukloniti sve reference na ovu vrijednost.");

        // Soft-delete: ne brišemo fizički, samo deaktiviramo
        entitet.Aktivan           = false;
        entitet.IzmijenjenDatum   = DateTime.UtcNow;
        entitet.IzmijenioKorisnik = korisnik;

        await _dbContext.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> DeleteCategory(string kategorija, string korisnik)
    {
        var normalizedCategory = kategorija?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCategory))
            return Result<bool>.ValidationError("Naziv šifrarnika ne smije biti prazan.");

        var definition = await _dbContext.CodeListDefinitions
            .AsTracking()
            .FirstOrDefaultAsync(item => item.Name == normalizedCategory);
        var values = await _dbContext.CodeLists
            .IgnoreQueryFilters()
            .AsTracking()
            .Where(item => item.Kategorija == normalizedCategory)
            .ToListAsync();

        if (definition is null && values.Count == 0)
            return Result<bool>.NotFoundError($"Šifrarnik '{normalizedCategory}' nije pronađen.");

        if (await JeKategorijaUUpotrebi(normalizedCategory, values.Select(item => item.Kod)))
            return Result<bool>.ValidationError(
                $"Šifrarnik '{normalizedCategory}' se ne može obrisati jer se najmanje jedna njegova vrijednost koristi u aplikaciji. " +
                "Prvo uklonite poslovne zapise koji koriste te vrijednosti.");

        var changedAt = DateTime.UtcNow;
        foreach (var value in values)
        {
            value.Aktivan = false;
            value.IzmijenjenDatum = changedAt;
            value.IzmijenioKorisnik = korisnik;
        }

        if (definition is not null)
            definition.IsActive = false;

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
        return kategorija switch
        {
            "OsnovPovezanosti" =>
                await _dbContext.LegalEntities.AsNoTracking().AnyAsync(entity => entity.BasisOfConnection == kod) ||
                await _dbContext.RelatedPersons.AsNoTracking().AnyAsync(person => person.RelationBasis == kod),
            "OsnovPosebnogOdnosa" =>
                await _dbContext.RelatedPersons.AsNoTracking().AnyAsync(person => person.SpecialRelationBasis == kod),
            "VrstaLimita" =>
                await _dbContext.Limiti.AsNoTracking().AnyAsync(limit => limit.TipLimita == kod),
            "Srodstvo" when int.TryParse(kod, out var relationshipType) =>
                await _dbContext.FamilyMembers.AsNoTracking().AnyAsync(member => (int)member.RelationshipType == relationshipType),
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
            "Srodstvo" =>
                await _dbContext.FamilyMembers.AsNoTracking().AnyAsync(member => codes.Contains(((int)member.RelationshipType).ToString())),
            _ => false
        };
    }

    private static Result<CodeListResponseDTO>? ValidateCreateDTO(CreateCodeListDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Kategorija))
            return Result<CodeListResponseDTO>.ValidationError("Kategorija ne smije biti prazna.");

        if (string.IsNullOrWhiteSpace(dto.Kod))
            return Result<CodeListResponseDTO>.ValidationError("Kod ne smije biti prazan.");

        if (string.IsNullOrWhiteSpace(dto.Naziv))
            return Result<CodeListResponseDTO>.ValidationError("Naziv ne smije biti prazan.");

        return null; // Validno
    }
}
