// File: src/RBBH.ConnectedParties/BL/ServiceInterfaces/ILegalEntityService.cs

using RBBH.ConnectedParties.DL.DTO;
using RBBH.ConnectedParties.DL.DTO.LegalEntity;

namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

/// <summary>
/// Servis za upravljanje registrom povezanih pravnih lica.
/// </summary>
public interface ILegalEntityService
{
    /// <summary>Vraća paginiranu listu aktivnih pravnih lica.</summary>
    Task<LegalEntityListDTO> GetAllAsync(int page, int pageSize, string? search);

    Task<List<LegalEntityDTO>> GetAllForExportAsync();

    /// <summary>Vraća detalje jednog pravnog lica po ID-u.</summary>
    Task<LegalEntityDTO?> GetByIdAsync(Guid id);

    /// <summary>Kreira novo pravno lice. Vraća kreirani entitet.</summary>
    Task<LegalEntityDTO> CreateAsync(CreateLegalEntityDTO dto, string createdBy);

    /// <summary>Ažurira postojeće pravno lice. Vraća ažurirani entitet.</summary>
    Task<LegalEntityDTO> UpdateAsync(Guid id, UpdateLegalEntityDTO dto, string modifiedBy);

    /// <summary>Soft delete — postavlja IsActive = false.</summary>
    Task DeleteAsync(Guid id, string deletedBy);

    /// <summary>Verifikuje pravno lice — mijenja status iz Draft u Verified.</summary>
    Task<LegalEntityDTO> VerifyAsync(Guid id, string verifiedBy);

    /// <summary>Pretražuje pravna lica za potrebe forme Limita po nazivu i matičnom broju.</summary>
    Task<List<LegalEntityLookupDTO>> SearchForLimitsAsync(string? search);

    /// <summary>Vraća podatke pravnog lica potrebne za automatsko popunjavanje forme Limita.</summary>
    Task<LegalEntityLimitFormDataDTO?> GetLimitFormDataAsync(Guid legalEntityId);

    /// <summary>Uvozi pravna lica iz Excel fajla (.xlsx).</summary>
    Task<ImportResultDTO> ImportFromExcelAsync(Stream stream, string createdBy);
}
