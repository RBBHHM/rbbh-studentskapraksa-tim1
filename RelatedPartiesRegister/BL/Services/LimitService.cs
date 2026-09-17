using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Limiti;
using RBBH.ConnectedParties.DL.Entities.Limiti;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions.Validations;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>
/// Implementacija CRUD operacija nad limitima.
/// </summary>
public class LimitService(ConnectedPartiesDbContext dbContext) : ILimitService
{
    private readonly ConnectedPartiesDbContext _dbContext = dbContext;

    private const int NazivMaxLength = 100;

    // ─── READ ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<List<LimitResponseDTO>>> GetAll()
    {
        var entities = await _dbContext.Limiti
            .AsNoTracking()
            .Include(x => x.LegalEntity)
            .OrderBy(x => x.Naziv)
            .ToListAsync();
        var capital = await CurrentCapital();
        var items = entities.Select(x => ToResponse(x, capital)).ToList();

        return Result<List<LimitResponseDTO>>.Success(items);
    }

    /// <inheritdoc/>
    public async Task<Result<LimitResponseDTO>> GetByID(int id)
    {
        if (id < 1)
            return Result<LimitResponseDTO>.ValidationError("ID nije validan.");

        var item = await _dbContext.Limiti
            .AsNoTracking()
            .Include(x => x.LegalEntity)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item is null)
            return Result<LimitResponseDTO>.NotFoundError($"Limit s ID={id} nije pronađen.");

        return Result<LimitResponseDTO>.Success(ToResponse(item, await CurrentCapital()));
    }

    // ─── CREATE ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<LimitResponseDTO>> Create(CreateLimitDTO dto, string korisnik)
    {
        var validacija = ValidateDto(
            dto.Naziv,
            dto.TipLimita,
            dto.IznosLimita,
            dto.MaksimalnoOcekivanaUtilizacija,
            0, 0);

        if (validacija is not null)
            return validacija;

        var configuredTypes = _dbContext.CodeLists.Where(item => item.Kategorija == "VrstaLimita");
        if (!await configuredTypes.AnyAsync(item => item.Kod == dto.TipLimita))
            return Result<LimitResponseDTO>.ValidationError("Odabrani tip limita nije aktivna vrijednost šifrarnika.");
        var client = dto.LegalEntityId.HasValue
            ? await _dbContext.LegalEntities.FirstOrDefaultAsync(item => item.Id == dto.LegalEntityId.Value)
            : null;
        if (dto.LegalEntityId.HasValue && client is null)
            return Result<LimitResponseDTO>.ValidationError("Odabrano pravno lice nije pronađeno.");

        var iznosLimita = dto.IznosLimita ?? 0;
        var maksimalnoOcekivanaUtilizacija = dto.MaksimalnoOcekivanaUtilizacija ?? 0;

        var entitet = new Limit
        {
            LegalEntityId = client?.Id,
            Naziv = client?.Name ?? dto.Naziv.Trim(),
            TipLimita = dto.TipLimita.Trim(),
            IznosLimita = iznosLimita,
            MaksimalnoOcekivanaUtilizacija = maksimalnoOcekivanaUtilizacija,
            KorigovaniLimit = dto.KorigovaniLimit,
            RokUtilizacije = dto.RokUtilizacije,
            Komentar = dto.Komentar?.Trim(),
            // Legacy non-null columns remain until a controlled DB migration; capital is sourced from Capital.
            CreatedAt = DateTime.UtcNow,
            CreatedBy = korisnik,
        };

        _dbContext.Limiti.Add(entitet);
        await _dbContext.SaveChangesAsync();

        entitet.LegalEntity = client;
        return Result<LimitResponseDTO>.Success(ToResponse(entitet, await CurrentCapital()));
    }

    // ─── UPDATE ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<LimitResponseDTO>> Update(int id, UpdateLimitDTO dto, string korisnik)
    {
        if (id < 1)
            return Result<LimitResponseDTO>.ValidationError("ID nije validan.");

        var validacija = ValidateDto(
            dto.Naziv,
            dto.TipLimita,
            dto.IznosLimita,
            dto.MaksimalnoOcekivanaUtilizacija,
            0, 0);

        if (validacija is not null)
            return validacija;

        var configuredTypes = _dbContext.CodeLists.Where(item => item.Kategorija == "VrstaLimita");
        if (!await configuredTypes.AnyAsync(item => item.Kod == dto.TipLimita))
            return Result<LimitResponseDTO>.ValidationError("Odabrani tip limita nije aktivna vrijednost šifrarnika.");

        var client = dto.LegalEntityId.HasValue
            ? await _dbContext.LegalEntities.FirstOrDefaultAsync(item => item.Id == dto.LegalEntityId.Value)
            : null;
        if (dto.LegalEntityId.HasValue && client is null)
            return Result<LimitResponseDTO>.ValidationError("Odabrano pravno lice nije pronađeno.");

        var entitet = await _dbContext.Limiti
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entitet is null)
            return Result<LimitResponseDTO>.NotFoundError($"Limit s ID={id} nije pronađen.");

        var iznosLimita = dto.IznosLimita ?? 0;
        var maksimalnoOcekivanaUtilizacija = dto.MaksimalnoOcekivanaUtilizacija ?? 0;

        entitet.LegalEntityId = client?.Id ?? entitet.LegalEntityId;
        entitet.Naziv = client?.Name ?? dto.Naziv.Trim();
        entitet.TipLimita = dto.TipLimita.Trim();
        entitet.IznosLimita = iznosLimita;
        entitet.MaksimalnoOcekivanaUtilizacija = maksimalnoOcekivanaUtilizacija;
        entitet.KorigovaniLimit = dto.KorigovaniLimit;
        entitet.RokUtilizacije = dto.RokUtilizacije;
        entitet.Komentar = dto.Komentar?.Trim();
        entitet.ModifiedAt = DateTime.UtcNow;
        entitet.ModifiedBy = korisnik;

        await _dbContext.SaveChangesAsync();

        entitet.LegalEntity = client ?? entitet.LegalEntity;
        return Result<LimitResponseDTO>.Success(ToResponse(entitet, await CurrentCapital()));
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

    /// <summary>
    /// Validacije: Naziv obavezan (max 100 karaktera), Tip limita obavezan,
    /// Iznos limita i maksimalno očekivana utilizacija ne mogu biti negativni,
    /// Regulatorni kapital obavezan broj, Osnovni kapital obavezan broj.
    /// </summary>
    private static Result<LimitResponseDTO>? ValidateDto(
        string naziv,
        string tipLimita,
        decimal? iznosLimita,
        decimal? maksimalnoOcekivanaUtilizacija,
        decimal? regulatorniKapital,
        decimal? osnovniKapital)
    {
        if (string.IsNullOrWhiteSpace(naziv))
            return Result<LimitResponseDTO>.ValidationError("Naziv je obavezan.");

        if (naziv.Trim().Length > NazivMaxLength)
            return Result<LimitResponseDTO>.ValidationError($"Naziv ne može imati više od {NazivMaxLength} karaktera.");

        if (string.IsNullOrWhiteSpace(tipLimita))
            return Result<LimitResponseDTO>.ValidationError("Tip limita je obavezan.");

        if (iznosLimita.HasValue && iznosLimita < 0)
            return Result<LimitResponseDTO>.ValidationError("Iznos limita ne može biti negativan.");

        if (maksimalnoOcekivanaUtilizacija.HasValue && maksimalnoOcekivanaUtilizacija < 0)
            return Result<LimitResponseDTO>.ValidationError("Maksimalno očekivana utilizacija ne može biti negativna.");

        if (regulatorniKapital is null)
            return Result<LimitResponseDTO>.ValidationError("Regulatorni kapital je obavezan i mora biti broj.");

        if (regulatorniKapital < 0)
            return Result<LimitResponseDTO>.ValidationError("Regulatorni kapital ne može biti negativan.");

        if (osnovniKapital is null)
            return Result<LimitResponseDTO>.ValidationError("Osnovni kapital je obavezan i mora biti broj.");

        if (osnovniKapital < 0)
            return Result<LimitResponseDTO>.ValidationError("Osnovni kapital ne može biti negativan.");

        return null;
    }

    private Task<RBBH.ConnectedParties.DL.Entities.Capital.Capital?> CurrentCapital() => _dbContext.Capitals.AsNoTracking()
        .Where(x => x.DatumKapitala <= DateTime.UtcNow.Date)
        .OrderByDescending(x => x.DatumKapitala).ThenByDescending(x => x.Id).FirstOrDefaultAsync();

    private static LimitResponseDTO ToResponse(Limit item, RBBH.ConnectedParties.DL.Entities.Capital.Capital? capital) => new()
    {
        Id = item.Id, LegalEntityId = item.LegalEntityId,
        IsResident = item.LegalEntity?.IsResident, FbaId = item.LegalEntity?.FbaId,
        TaxNumber = item.LegalEntity?.TaxNumber,
        MaticniBroj = item.LegalEntity?.Matbroj ?? item.LegalEntity?.MaticniBroj,
        GCCNumber = item.LegalEntity?.GCCNumber, GCCName = item.LegalEntity?.GCCName,
        LegalEntityName = item.LegalEntity?.Name ?? item.Naziv,
        Naziv = item.LegalEntity?.Name ?? item.Naziv, TipLimita = item.TipLimita,
        IznosLimita = item.IznosLimita, MaksimalnoOcekivanaUtilizacija = item.MaksimalnoOcekivanaUtilizacija,
        KorigovaniLimit = item.KorigovaniLimit, RokUtilizacije = item.RokUtilizacije, Komentar = item.Komentar,
        RegulatorniKapital = capital?.RegulatorniKapital, OsnovniKapital = capital?.OsnovniKapital,
        DatumKapitala = capital?.DatumKapitala,
        CreatedAt = item.CreatedAt, CreatedBy = item.CreatedBy, ModifiedAt = item.ModifiedAt, ModifiedBy = item.ModifiedBy
    };
}
