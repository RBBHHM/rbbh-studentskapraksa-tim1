using RBBH.ConnectedParties.DL.Entities.RelatedPersons;
using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.DTO.RelatedPersons;

// ─── CREATE ───────────────────────────────────────────────────────────────────

/// <summary>
/// DTO za dodavanje novog člana porodice na povezano fizičko lice.
/// </summary>
public class CreateFamilyMemberDTO
{
    [Required(ErrorMessage = "Ime je obavezno.")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Prezime je obavezno.")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rezidentnost je obavezna.")]
    public ResidencyType Residency { get; set; }

    /// <summary>Obavezan za rezidente (13 cifara).</summary>
    [StringLength(13)]
    public string? JMBG { get; set; }

    [StringLength(50)]
    public string? PassportNumber { get; set; }

    [StringLength(50)]
    public string? FBAId { get; set; }

    [Required(ErrorMessage = "Vrsta odnosa je obavezna.")]
    public FamilyRelationshipType RelationshipType { get; set; }

    /// <summary>
    /// Opciono: ID drugog člana porodice unutar iste porodice, kroz kojeg je ovaj
    /// član posredno povezan sa matičnim licem.
    /// null = direktna veza prema matičnom licu.
    /// </summary>
    public Guid? ParentFamilyMemberId { get; set; }
}

// ─── UPDATE ───────────────────────────────────────────────────────────────────

/// <summary>
/// DTO za izmjenu podataka člana porodice.
/// </summary>
public class UpdateFamilyMemberDTO
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

    [Required(ErrorMessage = "Vrsta odnosa je obavezna.")]
    public FamilyRelationshipType RelationshipType { get; set; }

    public Guid? ParentFamilyMemberId { get; set; }
}

// ─── RESPONSE ─────────────────────────────────────────────────────────────────

/// <summary>
/// Prikaz jednog člana porodice — koristi se i za ravnu listu i za izgradnju stabla.
/// </summary>
public class FamilyMemberResponseDTO
{
    public Guid Id { get; set; }

    public Guid RelatedPersonId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public ResidencyType Residency { get; set; }
    public string ResidencyLabel => Residency.ToString();

    public string? JMBG { get; set; }
    public string? PassportNumber { get; set; }
    public string? FBAId { get; set; }

    public FamilyRelationshipType RelationshipType { get; set; }
    public string RelationshipTypeLabel => RelationshipType.ToString();

    /// <summary>
    /// null = direktno dijete matičnog lica.
    /// Postavljeno = dijete/potomak nekog drugog člana porodice.
    /// Frontend gradi stablo grupišući po ovom polju.
    /// </summary>
    public Guid? ParentFamilyMemberId { get; set; }

    // ── Audit ──────────────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Djeca ovog člana u stablu — populiše se samo pri pozivu GetFamilyTree endpointa.
    /// U ravnoj listi ostaje null.
    /// </summary>
    public List<FamilyMemberResponseDTO>? Children { get; set; }
}
