using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.RelatedPersons;

/// <summary>
/// Povezano fizičko lice — osoba koja je povezana sa bankom ili nekim postojećim
/// subjektom u registru (npr. zaposlenik, vlasnik, povezano lice po posebnom odnosu...).
///
/// Identifikacija zavisi od <see cref="Residency"/>:
/// - Rezident: obavezan <see cref="JMBG"/> (13 cifara).
/// - Nerezident: JMBG nije obavezan; koristi se <see cref="PassportNumber"/> i/ili <see cref="FBAId"/>.
///
/// Svaki novokreirani zapis se čuva u statusu <see cref="RelatedPersonStatus.Draft"/>.
/// </summary>
public class RelatedPerson
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    #region Osnovni identifikacioni podaci

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Da li je lice rezident ili nerezident — određuje koja identifikacija je obavezna.</summary>
    [Required]
    public ResidencyType Residency { get; set; }

    /// <summary>
    /// Jedinstveni matični broj građana — obavezan i validira se (13 cifara) za rezidente.
    /// Za nerezidente nije obavezan.
    /// </summary>
    [StringLength(13)]
    public string? JMBG { get; set; }

    /// <summary>Broj pasoša — koristi se prvenstveno za nerezidente.</summary>
    [StringLength(50)]
    public string? PassportNumber { get; set; }

    /// <summary>FBA ID (identifikator iz registra FBA / drugog vanjskog registra) — koristi se za nerezidente.</summary>
    [StringLength(50)]
    public string? FBAId { get; set; }

    #endregion

    #region Podaci o povezanosti

    /// <summary>GCC broj.</summary>
    [StringLength(50)]
    public string? GCCNumber { get; set; }

    /// <summary>GCC naziv.</summary>
    [StringLength(250)]
    public string? GCCName { get; set; }

    /// <summary>Osnov povezanosti.</summary>
    [StringLength(500)]
    public string? RelationBasis { get; set; }

    /// <summary>Opis povezanosti.</summary>
    [StringLength(1000)]
    public string? RelationDescription { get; set; }

    /// <summary>Osnov posebnog odnosa.</summary>
    [StringLength(500)]
    public string? SpecialRelationBasis { get; set; }

    /// <summary>Datum od kada povezanost važi.</summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>Datum do kada povezanost važi.</summary>
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Označava da li je lice identifikovani zaposlenik — odgovara polju Is_Identified_Staff
    /// u HR sistemu (Employee tabela).
    /// </summary>
    public bool IsIdentifiedStaff { get; set; } = false;

    /// <summary>Lice je povezano sa bankom.</summary>
    public bool ConnectedWithBank { get; set; } = false;

    /// <summary>Lice je u posebnom odnosu sa bankom.</summary>
    public bool SpecialRelationshipWithBank { get; set; } = false;

    /// <summary>Postoji poseban ugovor sa licem.</summary>
    public bool SpecialContract { get; set; } = false;

    /// <summary>Lice je predmet malus/clawback klauzule.</summary>
    public bool MalusClawback { get; set; } = false;

    #endregion

    #region Porodica

    /// <summary>
    /// Izjava o nepostojanju članova porodice.
    /// Ako je true (DA) — unos članova porodice nije dozvoljen (kolekcija <see cref="FamilyMembers"/> mora ostati prazna).
    /// Ako je false (NE) — korisnik može unositi članove porodice.
    /// </summary>
    public bool DeclarationNoFamilyMembers { get; set; } = false;

    /// <summary>
    /// Postojeće fizičko lice preko kojeg je ovo lice evidentirano kao član uže porodice.
    /// Veza je samoreferentna kako bi svaka osoba postojala samo jednom u tabeli RelatedPersons.
    /// </summary>
    public Guid? RelatedToPersonId { get; set; }

    public virtual RelatedPerson? RelatedToPerson { get; set; }

    /// <summary>Vrsta porodičnog odnosa prema <see cref="RelatedToPerson"/>.</summary>
    public FamilyRelationshipType? FamilyRelationshipType { get; set; }

    /// <summary>Fizička lica koja su kroz porodični odnos povezana s ovim licem.</summary>
    public virtual ICollection<RelatedPerson> RelatedFamilyMembers { get; set; } = new List<RelatedPerson>();

    /// <summary>Članovi porodice vezani za ovo lice kao matično.</summary>
    public virtual ICollection<FamilyMember> FamilyMembers { get; set; } = new List<FamilyMember>();

    #endregion

    #region Status / radni proces

    /// <summary>Status zapisa kroz radni proces. Novokreirani zapisi su uvijek <see cref="RelatedPersonStatus.Draft"/>.</summary>
    [Required]
    public RelatedPersonStatus Status { get; set; } = RelatedPersonStatus.Draft;

    #endregion

    #region BaseEntity polja (audit / soft delete)

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? ModifiedAt { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }
    
    [StringLength(100)]
    public string? VerifiedBy { get; set; }

    public DateTime? VerifiedAt { get; set; }
    #endregion
}
