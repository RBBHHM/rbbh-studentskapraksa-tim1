using RBBH.ConnectedParties.DL.Entities.RelatedPersons;
using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.DTO.RelatedPersons;

// ─── CREATE ───────────────────────────────────────────────────────────────────

/// <summary>
/// DTO za kreiranje novog povezanog fizičkog lica.
/// </summary>
public class CreateRelatedPersonDTO
{
    [Required(ErrorMessage = "Ime je obavezno.")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Prezime je obavezno.")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rezidentnost je obavezna.")]
    public ResidencyType Residency { get; set; }

    /// <summary>
    /// Obavezan za rezidente (13 cifara). Neobavezan za nerezidente.
    /// </summary>
    [StringLength(13)]
    public string? JMBG { get; set; }

    [StringLength(50)]
    public string? PassportNumber { get; set; }

    [StringLength(50)]
    public string? FBAId { get; set; }

    // ── Podaci o povezanosti ──────────────────────────────────────────────────

    [StringLength(50)]
    public string? GCCNumber { get; set; }

    [StringLength(250)]
    public string? GCCName { get; set; }

    [StringLength(500)]
    public string? RelationBasis { get; set; }

    [StringLength(1000)]
    public string? RelationDescription { get; set; }

    [StringLength(500)]
    public string? SpecialRelationBasis { get; set; }

    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Da li je lice identifikovani zaposlenik.
    /// </summary>
    public bool IsIdentifiedStaff { get; set; } = false;

    /// <summary>
    /// Izjava o nepostojanju članova porodice.
    /// true = DA (unos članova porodice nije dozvoljen).
    /// false = NE (mogu se dodavati članovi porodice).
    /// </summary>
    public bool DeclarationNoFamilyMembers { get; set; } = false;

    public bool ConnectedWithBank { get; set; } = false;
    public bool SpecialRelationshipWithBank { get; set; } = false;
    public bool SpecialContract { get; set; } = false;
    public bool MalusClawback { get; set; } = false;

    public Guid? RelatedToPersonId { get; set; }
    public FamilyRelationshipType? FamilyRelationshipType { get; set; }
}

// ─── UPDATE ───────────────────────────────────────────────────────────────────

/// <summary>
/// DTO za izmjenu postojećeg povezanog fizičkog lica.
/// </summary>
public class UpdateRelatedPersonDTO
{
    [Required(ErrorMessage = "Ime je obavezno.")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Prezime je obavezno.")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rezidentnost je obavezna.")]
    public ResidencyType Residency { get; set; }

    [StringLength(13)]
    public string? JMBG { get; set; }

    [StringLength(50)]
    public string? PassportNumber { get; set; }

    [StringLength(50)]
    public string? FBAId { get; set; }

    [StringLength(50)]
    public string? GCCNumber { get; set; }

    [StringLength(250)]
    public string? GCCName { get; set; }

    [StringLength(500)]
    public string? RelationBasis { get; set; }

    [StringLength(1000)]
    public string? RelationDescription { get; set; }

    [StringLength(500)]
    public string? SpecialRelationBasis { get; set; }

    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    public bool IsIdentifiedStaff { get; set; } = false;

    public bool DeclarationNoFamilyMembers { get; set; } = false;

    public bool ConnectedWithBank { get; set; } = false;
    public bool SpecialRelationshipWithBank { get; set; } = false;
    public bool SpecialContract { get; set; } = false;
    public bool MalusClawback { get; set; } = false;

    public Guid? RelatedToPersonId { get; set; }
    public FamilyRelationshipType? FamilyRelationshipType { get; set; }
}

// ─── RESPONSE ─────────────────────────────────────────────────────────────────

/// <summary>
/// DTO koji se vraća klijentu — puni prikaz povezanog fizičkog lica.
/// </summary>
public class RelatedPersonResponseDTO
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public ResidencyType Residency { get; set; }
    public string ResidencyLabel => Residency.ToString();

    public string? JMBG { get; set; }
    public string? PassportNumber { get; set; }
    public string? FBAId { get; set; }

    public string? GCCNumber { get; set; }
    public string? GCCName { get; set; }
    public string? RelationBasis { get; set; }
    public string? RelationDescription { get; set; }
    public string? SpecialRelationBasis { get; set; }

    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    public bool IsIdentifiedStaff { get; set; }
    public bool DeclarationNoFamilyMembers { get; set; }

    public bool ConnectedWithBank { get; set; }
    public bool SpecialRelationshipWithBank { get; set; }
    public bool SpecialContract { get; set; }
    public bool MalusClawback { get; set; }

    public Guid? RelatedToPersonId { get; set; }
    public string? RelatedToPersonName { get; set; }
    public FamilyRelationshipType? FamilyRelationshipType { get; set; }
    public string? FamilyRelationshipTypeLabel => FamilyRelationshipType?.ToString();

    public RelatedPersonStatus Status { get; set; }
    public string StatusLabel => Status.ToString();

    // ── Audit ──────────────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    
    /// <summary>Broj aktivnih članova porodice — korisno za listu bez učitavanja cijele kolekcije.</summary>
    public int FamilyMemberCount { get; set; }
    public string PersonType => RelatedToPersonId.HasValue ? "FamilyMember" : IsIdentifiedStaff ? "Employee" : "RelatedPerson";
    public string PersonTypeLabel => RelatedToPersonId.HasValue ? "Član porodice" : IsIdentifiedStaff ? "Zaposlenik" : "Povezano lice";
}

/// <summary>
/// Skraćeni prikaz za liste / padajuće menije.
/// </summary>
public class RelatedPersonSummaryDTO
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? JMBG { get; set; }
    public RelatedPersonStatus Status { get; set; }
    public string StatusLabel => Status.ToString();
    public ResidencyType Residency { get; set; }
    public int FamilyMemberCount { get; set; }
    public bool IsIdentifiedStaff { get; set; }
    public Guid? RelatedToPersonId { get; set; }
    public string PersonType => RelatedToPersonId.HasValue ? "FamilyMember" : IsIdentifiedStaff ? "Employee" : "RelatedPerson";
    public string PersonTypeLabel => RelatedToPersonId.HasValue ? "Član porodice" : IsIdentifiedStaff ? "Zaposlenik" : "Povezano lice";
    public DateTime CreatedAt { get; set; }
}

public sealed class RelatedPersonTreeNodeDTO
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PersonType { get; set; } = string.Empty;
    public string PersonTypeLabel { get; set; } = string.Empty;
    public FamilyRelationshipType? RelationshipType { get; set; }
    public string? RelationshipTypeLabel => RelationshipType?.ToString();
    public List<RelatedPersonTreeNodeDTO> Children { get; set; } = [];
}

public sealed class DuplicateIdentityResponseDTO
{
    public bool Exists { get; set; }
    public string? Field { get; set; }
    public string? Message { get; set; }
}
