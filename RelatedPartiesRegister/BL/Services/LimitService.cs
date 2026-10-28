using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Limiti;
using RBBH.ConnectedParties.DL.Entities.Limiti;
using RBBH.ConnectedParties.DL.Entities.LegalEntity;
using RBBH.ConnectedParties.DL.Entities.Report;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions.Validations;
using Microsoft.EntityFrameworkCore;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>
/// Implementacija CRUD operacija nad limitima.
/// </summary>
public class LimitService(ConnectedPartiesDbContext dbContext) : ILimitService
{
    private readonly ConnectedPartiesDbContext _dbContext = dbContext;

    // ─── READ ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<List<LimitResponseDTO>>> GetAll(Guid? legalEntityId = null, string? matbroj = null)
    {
        var query = BaseQuery();

        if (legalEntityId.HasValue)
            query = query.Where(x => x.Limit.LegalEntityId == legalEntityId.Value);
        else if (!string.IsNullOrWhiteSpace(matbroj))
            query = query.Where(x => x.Entity.MaticniBroj == matbroj.Trim() || x.Entity.Matbroj == matbroj.Trim());

        var capital = await GetGlobalCapitalAsync();

        var items = await query
            .OrderBy(x => x.Entity.Name)
            .Select(x => ToResponseDto(x.Limit, x.Entity))
            .ToListAsync();

        foreach (var item in items)
            ApplyCapital(item, capital);

        return Result<List<LimitResponseDTO>>.Success(items);
    }

    /// <inheritdoc/>
    public async Task<Result<LimitResponseDTO>> GetByID(int id)
    {
        if (id < 1)
            return Result<LimitResponseDTO>.ValidationError("ID nije validan.");

        var item = await BaseQuery()
            .Where(x => x.Limit.Id == id)
            .Select(x => ToResponseDto(x.Limit, x.Entity))
            .FirstOrDefaultAsync();

        if (item is null)
            return Result<LimitResponseDTO>.NotFoundError($"Limit s ID={id} nije pronađen.");

        ApplyCapital(item, await GetGlobalCapitalAsync());

        return Result<LimitResponseDTO>.Success(item);
    }

    // ─── CREATE ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<LimitResponseDTO>> Create(CreateLimitDTO dto, string korisnik)
    {
        var validacija = await ValidateDto(dto.LegalEntityId, dto.TipLimita, dto.OdobreniLimit, dto.MaksimalnaOcekivanaUtilizacija);
        if (validacija is not null)
            return validacija;

        var entitet = new Limit
        {
            LegalEntityId = dto.LegalEntityId,
            TipLimita = dto.TipLimita.Trim().ToUpper(),
            OdobreniLimit = dto.OdobreniLimit,
            MaksimalnaOcekivanaUtilizacija = dto.MaksimalnaOcekivanaUtilizacija,
            RokOcekivaneUtilizacije = dto.RokOcekivaneUtilizacije,
            Komentar = dto.Komentar?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = korisnik,
        };

        _dbContext.Limiti.Add(entitet);
        await _dbContext.SaveChangesAsync();

        return await GetByID(entitet.Id);
    }

    // ─── UPDATE ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<LimitResponseDTO>> Update(int id, UpdateLimitDTO dto, string korisnik)
    {
        if (id < 1)
            return Result<LimitResponseDTO>.ValidationError("ID nije validan.");

        var validacija = await ValidateDto(dto.LegalEntityId, dto.TipLimita, dto.OdobreniLimit, dto.MaksimalnaOcekivanaUtilizacija);
        if (validacija is not null)
            return validacija;

        var entitet = await _dbContext.Limiti
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entitet is null)
            return Result<LimitResponseDTO>.NotFoundError($"Limit s ID={id} nije pronađen.");

        entitet.LegalEntityId = dto.LegalEntityId;
        entitet.TipLimita = dto.TipLimita.Trim().ToUpper();
        entitet.OdobreniLimit = dto.OdobreniLimit;
        entitet.MaksimalnaOcekivanaUtilizacija = dto.MaksimalnaOcekivanaUtilizacija;
        entitet.RokOcekivaneUtilizacije = dto.RokOcekivaneUtilizacije;
        entitet.Komentar = dto.Komentar?.Trim();
        entitet.ModifiedAt = DateTime.UtcNow;
        entitet.ModifiedBy = korisnik;

        await _dbContext.SaveChangesAsync();

        return await GetByID(entitet.Id);
    }

    // ─── DELETE ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<bool>> Delete(int id)
    {
        if (id < 1)
            return Result<bool>.ValidationError("ID nije validan.");

        var entitet = await _dbContext.Limiti
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entitet is null)
            return Result<bool>.NotFoundError($"Limit s ID={id} nije pronađen.");

        _dbContext.Limiti.Remove(entitet);
        await _dbContext.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    // ─── Privatne metode ────────────────────────────────────────────────────

    private sealed class LimitJoinRow
    {
        public required Limit Limit { get; init; }
        public required LegalEntity Entity { get; init; }
    }

    private IQueryable<LimitJoinRow> BaseQuery() =>
        from limit in _dbContext.Limiti.AsNoTracking()
        join entity in _dbContext.LegalEntities.AsNoTracking() on limit.LegalEntityId equals entity.Id
        select new LimitJoinRow { Limit = limit, Entity = entity };

    /// <summary>Kapital je jedan globalni zapis — čita se odvojeno i primjenjuje na svaki Limit red.</summary>
    private async Task<Capital?> GetGlobalCapitalAsync() =>
        await _dbContext.Capitals.AsNoTracking().FirstOrDefaultAsync();

    private static void ApplyCapital(LimitResponseDTO dto, Capital? capital)
    {
        dto.RegulatorniKapital = capital?.RegulatoryCapital;
        dto.OsnovniKapital = capital?.CoreCapital;
        dto.DatumKapitala = capital?.CapitalDate;
    }

    private static LimitResponseDTO ToResponseDto(Limit limit, LegalEntity entity) => new()
    {
        Id = limit.Id,
        LegalEntityId = limit.LegalEntityId,
        Naziv = entity.Name,
        IsResident = entity.IsResident,
        FbaId = entity.FbaId,
        TaxNumber = entity.TaxNumber,
        MaticniBroj = entity.MaticniBroj,
        Matbroj = entity.Matbroj,
        TipLimita = limit.TipLimita,
        OdobreniLimit = limit.OdobreniLimit,
        MaksimalnaOcekivanaUtilizacija = limit.MaksimalnaOcekivanaUtilizacija,
        RokOcekivaneUtilizacije = limit.RokOcekivaneUtilizacije,
        Komentar = limit.Komentar,
        CreatedAt = limit.CreatedAt,
        CreatedBy = limit.CreatedBy,
        ModifiedAt = limit.ModifiedAt,
        ModifiedBy = limit.ModifiedBy,
    };

    /// <summary>
    /// Validacije: pravno lice mora postojati, tip limita je obavezan i mora postojati
    /// kao aktivna vrijednost šifarnika VrstaLimita, iznosi ne mogu biti negativni.
    /// </summary>
    private async Task<Result<LimitResponseDTO>?> ValidateDto(
        Guid legalEntityId,
        string tipLimita,
        decimal? odobreniLimit,
        decimal? maksimalnaOcekivanaUtilizacija)
    {
        if (legalEntityId == Guid.Empty)
            return Result<LimitResponseDTO>.ValidationError("Pravno lice je obavezno.");

        var legalEntityExists = await _dbContext.LegalEntities.AsNoTracking()
            .AnyAsync(e => e.Id == legalEntityId && e.IsActive);
        if (!legalEntityExists)
            return Result<LimitResponseDTO>.ValidationError("Odabrano pravno lice ne postoji.");

        if (string.IsNullOrWhiteSpace(tipLimita))
            return Result<LimitResponseDTO>.ValidationError("Tip limita je obavezan.");

        var kod = tipLimita.Trim().ToUpper();
        var tipLimitaPostoji = await _dbContext.CodeLists.AsNoTracking()
            .AnyAsync(c => c.Kategorija == "VrstaLimita" && c.Kod == kod && c.Aktivan);
        if (!tipLimitaPostoji)
            return Result<LimitResponseDTO>.ValidationError($"Tip limita '{tipLimita}' ne postoji u šifarniku vrsta limita.");

        if (odobreniLimit.HasValue && odobreniLimit < 0)
            return Result<LimitResponseDTO>.ValidationError("Odobreni limit ne može biti negativan.");

        if (maksimalnaOcekivanaUtilizacija.HasValue && maksimalnaOcekivanaUtilizacija < 0)
            return Result<LimitResponseDTO>.ValidationError("Maksimalna očekivana utilizacija ne može biti negativna.");

        return null;
    }
}
