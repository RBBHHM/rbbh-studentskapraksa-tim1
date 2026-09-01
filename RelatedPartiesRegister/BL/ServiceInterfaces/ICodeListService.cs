using RBBH.ConnectedParties.DL.DTO.Sifarnici;
using RBBH.ConnectedParties.Exceptions.Validations;

namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

/// <summary>
/// PL-36: CRUD operacije nad šifarnikom.
/// </summary>
public interface ICodeListService
{
    /// <summary>Vraća listu svih distinktnih kategorija.</summary>
    Task<Result<object>> GetCategoriesAsync();

    /// <summary>Vraća sve aktivne vrijednosti za datu kategoriju (za padajuće menije).</summary>
    Task<Result<List<CodeListDropdownDTO>>> GetDropdownByKategorija(string kategorija);

    /// <summary>Vraća sve vrijednosti za datu kategoriju (s audit podacima, za administraciju).</summary>
    Task<Result<List<CodeListResponseDTO>>> GetAllByKategorija(string kategorija);

    /// <summary>Vraća jednu vrijednost po ID-u.</summary>
    Task<Result<CodeListResponseDTO>> GetByID(int id);

    /// <summary>Kreira novu vrijednost šifarnika.</summary>
    /// <param name="dto">Podaci nove vrijednosti.</param>
    /// <param name="korisnik">Prijavljeni korisnik (iz HttpContext).</param>
    Task<Result<CodeListResponseDTO>> Create(CreateCodeListDTO dto, string korisnik);

    /// <summary>Ažurira naziv, opis i redoslijed prikaza (Kod i Kategorija se ne mijenjaju).</summary>
    /// <param name="id">ID zapisa koji se ažurira.</param>
    /// <param name="dto">Novi podaci.</param>
    /// <param name="korisnik">Prijavljeni korisnik (iz HttpContext).</param>
    Task<Result<CodeListResponseDTO>> Update(int id, UpdateCodeListDTO dto, string korisnik);

    /// <summary>
    /// PL-37: Brisanje — ako je vrijednost u upotrebi, vraća grešku s upozorenjem.
    /// Inače radi soft-delete (Aktivan = false).
    /// </summary>
    /// <param name="id">ID zapisa koji se briše.</param>
    /// <param name="korisnik">Prijavljeni korisnik.</param>
    Task<Result<bool>> Delete(int id, string korisnik);

    /// <summary>
    /// Briše cijelu definiciju šifrarnika i sve njene vrijednosti metodom soft-delete.
    /// Brisanje nije dozvoljeno dok je bilo koja vrijednost šifrarnika u upotrebi.
    /// </summary>
    /// <param name="kategorija">Naziv kategorije koja se briše.</param>
    /// <param name="korisnik">Prijavljeni korisnik.</param>
    Task<Result<bool>> DeleteCategory(string kategorija, string korisnik);
}
