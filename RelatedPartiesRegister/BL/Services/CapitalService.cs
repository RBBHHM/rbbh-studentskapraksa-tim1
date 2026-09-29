using RBBH.ConnectedParties.BL.ServiceInterfaces;
using RBBH.ConnectedParties.DL.DTO.Report;
using RBBH.ConnectedParties.DL.Entities.Report;
using RBBH.ConnectedParties.DL.Persistence;
using RBBH.ConnectedParties.Exceptions.Validations;
using Microsoft.EntityFrameworkCore;

namespace RBBH.ConnectedParties.BL.Services;

/// <summary>
/// Implementacija modula Kapital nad entitetom <see cref="Capital"/> — jedan globalni zapis.
/// </summary>
public class CapitalService(ConnectedPartiesDbContext dbContext) : ICapitalService
{
    private readonly ConnectedPartiesDbContext _dbContext = dbContext;

    /// <inheritdoc/>
    public async Task<Result<CapitalDTO?>> Get()
    {
        var capital = await _dbContext.Capitals.AsNoTracking().FirstOrDefaultAsync();

        return Result<CapitalDTO?>.Success(capital is null ? null : ToDto(capital));
    }

    /// <inheritdoc/>
    public async Task<Result<CapitalDTO>> Upsert(UpdateCapitalDTO dto, string korisnik)
    {
        if (!dto.RegulatoryCapital.HasValue || dto.RegulatoryCapital < 0)
            return Result<CapitalDTO>.ValidationError("Regulatorni kapital je obavezan i ne može biti negativan.");

        if (!dto.CoreCapital.HasValue || dto.CoreCapital < 0)
            return Result<CapitalDTO>.ValidationError("Osnovni kapital je obavezan i ne može biti negativan.");

        if (!dto.SupplementaryCapital.HasValue || dto.SupplementaryCapital < 0)
            return Result<CapitalDTO>.ValidationError("Dopunski kapital je obavezan i ne može biti negativan.");

        if (!dto.CapitalDate.HasValue)
            return Result<CapitalDTO>.ValidationError("Datum kapitala je obavezan.");

        var capital = await _dbContext.Capitals.AsTracking().FirstOrDefaultAsync();

        if (capital is null)
        {
            capital = new Capital
            {
                RegulatoryCapital = dto.RegulatoryCapital.Value,
                CoreCapital = dto.CoreCapital.Value,
                SupplementaryCapital = dto.SupplementaryCapital.Value,
                CapitalDate = dto.CapitalDate.Value,
                CreatedBy = korisnik,
                CreatedAt = DateTime.UtcNow,
            };
            _dbContext.Capitals.Add(capital);
        }
        else
        {
            capital.RegulatoryCapital = dto.RegulatoryCapital.Value;
            capital.CoreCapital = dto.CoreCapital.Value;
            capital.SupplementaryCapital = dto.SupplementaryCapital.Value;
            capital.CapitalDate = dto.CapitalDate.Value;
            capital.ModifiedAt = DateTime.UtcNow;
            capital.ModifiedBy = korisnik;
        }

        await _dbContext.SaveChangesAsync();

        return Result<CapitalDTO>.Success(ToDto(capital));
    }

    private static CapitalDTO ToDto(Capital capital) => new()
    {
        Id = capital.Id,
        RegulatoryCapital = capital.RegulatoryCapital,
        CoreCapital = capital.CoreCapital,
        SupplementaryCapital = capital.SupplementaryCapital,
        CapitalDate = capital.CapitalDate,
    };
}
