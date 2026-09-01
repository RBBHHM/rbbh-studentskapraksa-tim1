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
        var items = await _dbContext.Limiti
            .AsNoTracking()
            .OrderBy(x => x.Naziv)
            .ProjectToType<LimitResponseDTO>()
            .ToListAsync();

        return Result<List<LimitResponseDTO>>.Success(items);
    }

    /// <inheritdoc/>
    public async Task<Result<LimitResponseDTO>> GetByID(int id)
    {
        if (id < 1)
            return Result<LimitResponseDTO>.ValidationError("ID nije validan.");

        var item = await _dbContext.Limiti
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item is null)
            return Result<LimitResponseDTO>.NotFoundError($"Limit s ID={id} nije pronađen.");

        return Result<LimitResponseDTO>.Success(item.Adapt<LimitResponseDTO>());
    }

    // ─── CREATE ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Result<LimitResponseDTO>> Create(CreateLimitDTO dto, string korisnik)
    {
        var validacija = ValidateDto(
            dto.Naziv,
            dto.TipLimita,
            dto.IznosLimita,
            dto.Utilizacija,
            dto.RegulatorniKapital ?? 0,
            dto.OsnovniKapital ?? 0);

        if (validacija is not null)
            return validacija;

        var iznosLimita = dto.IznosLimita ?? 0;
        var utilizacija = dto.Utilizacija ?? 0;
        var raspoloziviLimit = IzracunajRaspoloziviLimit(iznosLimita, utilizacija, dto.KorigovaniLimit);

        var entitet = new Limit
        {
            Naziv = dto.Naziv.Trim(),
            TipLimita = dto.TipLimita.Trim(),
            IznosLimita = iznosLimita,
            Utilizacija = utilizacija,
            KorigovaniLimit = dto.KorigovaniLimit,
            RaspoloziviLimit = raspoloziviLimit,
            RokUtilizacije = dto.RokUtilizacije,
            Komentar = dto.Komentar?.Trim(),
            RegulatorniKapital = dto.RegulatorniKapital ?? 0,
            OsnovniKapital = dto.OsnovniKapital ?? 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = korisnik,
        };

        _dbContext.Limiti.Add(entitet);
        await _dbContext.SaveChangesAsync();

        return Result<LimitResponseDTO>.Success(entitet.Adapt<LimitResponseDTO>());
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
            dto.Utilizacija,
            dto.RegulatorniKapital ?? 0,
            dto.OsnovniKapital ?? 0);

        if (validacija is not null)
            return validacija;

        var entitet = await _dbContext.Limiti
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entitet is null)
            return Result<LimitResponseDTO>.NotFoundError($"Limit s ID={id} nije pronađen.");

        var iznosLimita = dto.IznosLimita ?? 0;
        var utilizacija = dto.Utilizacija ?? 0;
        var raspoloziviLimit = IzracunajRaspoloziviLimit(iznosLimita, utilizacija, dto.KorigovaniLimit);

        entitet.Naziv = dto.Naziv.Trim();
        entitet.TipLimita = dto.TipLimita.Trim();
        entitet.IznosLimita = iznosLimita;
        entitet.Utilizacija = utilizacija;
        entitet.KorigovaniLimit = dto.KorigovaniLimit;
        entitet.RaspoloziviLimit = raspoloziviLimit;
        entitet.RokUtilizacije = dto.RokUtilizacije;
        entitet.Komentar = dto.Komentar?.Trim();
        if (dto.RegulatorniKapital.HasValue) entitet.RegulatorniKapital = dto.RegulatorniKapital.Value;
        if (dto.OsnovniKapital.HasValue) entitet.OsnovniKapital = dto.OsnovniKapital.Value;
        entitet.ModifiedAt = DateTime.UtcNow;
        entitet.ModifiedBy = korisnik;

        await _dbContext.SaveChangesAsync();

        return Result<LimitResponseDTO>.Success(entitet.Adapt<LimitResponseDTO>());
    }

    public async Task<Result<LimitResponseDTO>> UpdateCapital(int id, UpdateCapitalDTO dto, string korisnik)
    {
        if (id < 1) return Result<LimitResponseDTO>.ValidationError("ID nije validan.");
        if (!dto.RegulatorniKapital.HasValue || dto.RegulatorniKapital < 0)
            return Result<LimitResponseDTO>.ValidationError("Regulatorni kapital je obavezan i ne može biti negativan.");
        if (!dto.OsnovniKapital.HasValue || dto.OsnovniKapital < 0)
            return Result<LimitResponseDTO>.ValidationError("Osnovni kapital je obavezan i ne može biti negativan.");

        var entity = await _dbContext.Limiti.AsTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (entity is null) return Result<LimitResponseDTO>.NotFoundError($"Limit s ID={id} nije pronađen.");

        entity.RegulatorniKapital = dto.RegulatorniKapital.Value;
        entity.OsnovniKapital = dto.OsnovniKapital.Value;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = korisnik;
        await _dbContext.SaveChangesAsync();
        return Result<LimitResponseDTO>.Success(entity.Adapt<LimitResponseDTO>());
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
    /// Automatski obračun raspoloživog limita:
    /// ako je korigovani limit popunjen koristi se on, inače se koristi iznos limita.
    /// </summary>
    private static decimal IzracunajRaspoloziviLimit(
        decimal iznosLimita,
        decimal utilizacija,
        decimal? korigovaniLimit)
    {
        var osnovica = korigovaniLimit ?? iznosLimita;
        return osnovica - utilizacija;
    }

    /// <summary>
    /// Validacije: Naziv obavezan (max 100 karaktera), Tip limita obavezan,
    /// Iznos limita obavezan broj, Utilizacija obavezan broj,
    /// Regulatorni kapital obavezan broj, Osnovni kapital obavezan broj.
    /// </summary>
    private static Result<LimitResponseDTO>? ValidateDto(
        string naziv,
        string tipLimita,
        decimal? iznosLimita,
        decimal? utilizacija,
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

        if (utilizacija.HasValue && utilizacija < 0)
            return Result<LimitResponseDTO>.ValidationError("Utilizacija ne može biti negativna.");

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
}
