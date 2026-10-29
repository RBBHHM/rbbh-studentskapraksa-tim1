using RBBH.ConnectedParties.DL.DTO;
using RBBH.ConnectedParties.DL.DTO.RelatedPersons;
using RBBH.ConnectedParties.Exceptions.Validations;

namespace RBBH.ConnectedParties.BL.ServiceInterfaces;

/// <summary>
/// Servis za evidenciju povezanih fizičkih lica i njihovih članova porodice.
/// </summary>
public interface IRelatedPersonService
{
    // ─── RelatedPerson — READ ───────────────────────────────────────────────

    /// <summary>Vraća listu svih povezanih fizičkih lica (skraćeni prikaz).</summary>
    Task<Result<List<RelatedPersonSummaryDTO>>> GetAll();

    /// <summary>Vraća listu svih povezanih fizičkih lica s punim podacima (za export).</summary>
    Task<Result<List<RelatedPersonResponseDTO>>> GetAllDetailed();

    /// <summary>Vraća jedno povezano fizičko lice po ID-u (puni prikaz).</summary>
    Task<Result<RelatedPersonResponseDTO>> GetById(Guid id);

    Task<Result<List<RelatedPersonTreeNodeDTO>>> GetRelationshipTree(Guid relatedPersonId);

    Task<DuplicateIdentityResponseDTO> CheckDuplicateIdentity(string? jmbg, string? passportNumber, string? fbaId, Guid? excludeId = null);

    // ─── RelatedPerson — CREATE / UPDATE / DELETE ───────────────────────────

    /// <summary>
    /// Kreira novo povezano fizičko lice. Validira identifikaciju prema rezidentnosti
    /// (JMBG za rezidente, pasoš/FBA ID za nerezidente). Novi zapis se uvijek
    /// kreira u statusu <see cref="DL.Entities.RelatedPersons.RelatedPersonStatus.Draft"/>.
    /// </summary>
    Task<Result<RelatedPersonResponseDTO>> Create(CreateRelatedPersonDTO dto, string korisnik);

    /// <summary>Ažurira postojeće povezano fizičko lice.</summary>
    Task<Result<RelatedPersonResponseDTO>> Update(Guid id, UpdateRelatedPersonDTO dto, string korisnik);

    /// <summary>Soft-delete povezanog fizičkog lica (i svih njegovih aktivnih članova porodice).</summary>
    Task<Result<bool>> Delete(Guid id, string korisnik);

    /// <summary>Verifikuje povezano fizičko lice — mijenja status iz Draft u Verified.</summary>
    Task<Result<RelatedPersonResponseDTO>> Verify(Guid id, string korisnik);

    // ─── FamilyMember — READ ─────────────────────────────────────────────────

    /// <summary>Vraća ravnu listu članova porodice za dato matično lice.</summary>
    Task<Result<List<FamilyMemberResponseDTO>>> GetFamilyMembers(Guid relatedPersonId);

    /// <summary>Vraća sve aktivne članove porodice za sva matična lica (za export).</summary>
    Task<Result<List<FamilyMemberResponseDTO>>> GetAllFamilyMembers();

    /// <summary>
    /// Vraća porodično stablo za dato matično lice — članovi porodice organizovani
    /// hijerarhijski prema <see cref="DL.Entities.RelatedPersons.FamilyMember.ParentFamilyMemberId"/>.
    /// Vraća samo "korijenske" članove (direktno povezane sa matičnim licem),
    /// svaki sa popunjenom kolekcijom <see cref="FamilyMemberResponseDTO.Children"/>.
    /// </summary>
    Task<Result<List<FamilyMemberResponseDTO>>> GetFamilyTree(Guid relatedPersonId);

    // ─── FamilyMember — CREATE / UPDATE / DELETE ────────────────────────────

    /// <summary>
    /// Dodaje novog člana porodice matičnom licu. Vraća grešku ako matično lice
    /// ima postavljenu izjavu o nepostojanju članova porodice (DeclarationNoFamilyMembers = true).
    /// </summary>
    Task<Result<FamilyMemberResponseDTO>> AddFamilyMember(Guid relatedPersonId, CreateFamilyMemberDTO dto, string korisnik);

    /// <summary>Ažurira podatke postojećeg člana porodice.</summary>
    Task<Result<FamilyMemberResponseDTO>> UpdateFamilyMember(Guid relatedPersonId, Guid familyMemberId, UpdateFamilyMemberDTO dto, string korisnik);

    /// <summary>Soft-delete člana porodice.</summary>
    Task<Result<bool>> DeleteFamilyMember(Guid relatedPersonId, Guid familyMemberId, string korisnik);

    /// <summary>
    /// Uvozi fizička lica iz Excel fajla (.xlsx). Redovi koji ne prođu validaciju se preskače
    /// i bilježe u rezultatu. Vraća broj uvezenih i broj grešaka.
    /// </summary>
    Task<ImportResultDTO> ImportFromExcelAsync(Stream stream, string createdBy);
}
