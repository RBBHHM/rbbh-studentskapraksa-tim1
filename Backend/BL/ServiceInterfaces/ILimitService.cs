using RBBH.ConnectedParties.DL.DTO.Limiti;
using RBBH.ConnectedParties.Exceptions.Validations;

namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

/// <summary>
/// CRUD operacije nad limitima.
/// </summary>
public interface ILimitService
{
    /// <summary>Vraća limite, opcionalno filtrirano po pravnom licu ili matičnom broju.</summary>
    Task<BL.Services.ReferencePage<LimitResponseDTO>> Page(Guid? legalEntityId, string? search, int page, int pageSize, CancellationToken ct, DateTime? asOf = null);
    Task<Result<List<LimitResponseDTO>>> GetAll(Guid? legalEntityId = null, string? matbroj = null);

    Task<Result<List<LimitResponseDTO>>> ExportData(Guid? legalEntityId, string? matbroj, DateTime? asOf, CancellationToken ct);
    Task<Result<List<LimitResponseDTO>>> GetAsOf(Guid? legalEntityId, string? matbroj, DateTime asOf, CancellationToken ct);

    /// <summary>Vraća jedan limit po ID-u.</summary>
    Task<Result<LimitResponseDTO>> GetByID(int id);

    /// <summary>Kreira novi limit.</summary>
    /// <param name="dto">Podaci novog limita.</param>
    /// <param name="korisnik">Prijavljeni korisnik (iz HttpContext).</param>
    Task<Result<LimitResponseDTO>> Create(CreateLimitDTO dto, string korisnik);

    /// <summary>Ažurira postojeći limit.</summary>
    /// <param name="id">ID limita koji se ažurira.</param>
    /// <param name="dto">Novi podaci.</param>
    /// <param name="korisnik">Prijavljeni korisnik (iz HttpContext).</param>
    Task<Result<LimitResponseDTO>> Update(int id, UpdateLimitDTO dto, string korisnik);

    /// <summary>Briše limit.</summary>
    /// <param name="id">ID limita koji se briše.</param>
    Task<Result<bool>> Delete(int id);
    Task<Result<bool>> Delete(int id, string korisnik);
    Task<DL.DTO.ImportResultDTO> Import(Stream stream, string korisnik, CancellationToken ct);
}
