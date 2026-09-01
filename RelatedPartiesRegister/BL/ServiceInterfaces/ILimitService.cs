using RBBH.ConnectedParties.DL.DTO.Limiti;
using RBBH.ConnectedParties.Exceptions.Validations;

namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

/// <summary>
/// CRUD operacije nad limitima.
/// </summary>
public interface ILimitService
{
    /// <summary>Vraća sve limite.</summary>
    Task<Result<List<LimitResponseDTO>>> GetAll();

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

    Task<Result<LimitResponseDTO>> UpdateCapital(int id, UpdateCapitalDTO dto, string korisnik);
}
