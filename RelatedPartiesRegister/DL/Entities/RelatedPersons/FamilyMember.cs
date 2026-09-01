using System.ComponentModel.DataAnnotations;

namespace RBBH.ConnectedParties.DL.Entities.RelatedPersons;

/// <summary>
/// Član porodice povezanog fizičkog lica.
///
/// Svaki član porodice se veže za matično <see cref="RelatedPerson"/> (preko <see cref="RelatedPersonId"/>)
/// i čuva vlastite identifikacione podatke i vrstu odnosa prema matičnom licu.
///
/// Unos je dozvoljen samo ako matično lice ima <see cref="RelatedPerson.DeclarationNoFamilyMembers"/> = false.
///
/// Hijerarhijski prikaz porodičnog stabla:
/// Pored direktne veze prema matičnom licu, svaki član porodice može imati i referencu
/// na drugog člana porodice (<see cref="ParentFamilyMemberId"/>) preko kojeg je povezan
/// sa matičnim licem (npr. dijete → bračni partner koji je takođe evidentiran kao član porodice,
/// unuk → dijete, itd.). Ovo omogućava da se porodica prikaže kao stablo (ne samo ravna lista)
/// bez ikakve buduće promjene šeme baze — frontend samo grupiše zapise po
/// <see cref="ParentFamilyMemberId"/> (null = direktno dijete matičnog lica).
/// </summary>
public class FamilyMember
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Matično (povezano) fizičko lice za koje se vodi porodica.</summary>
    [Required]
    public Guid RelatedPersonId { get; set; }

    public virtual RelatedPerson? RelatedPerson { get; set; }

    #region Identifikacija člana porodice

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Da li je član porodice rezident ili nerezident — određuje koja identifikacija je relevantna.</summary>
    [Required]
    public ResidencyType Residency { get; set; }

    /// <summary>JMBG člana porodice (obavezan i validira se za rezidente — 13 cifara).</summary>
    [StringLength(13)]
    public string? JMBG { get; set; }

    /// <summary>Broj pasoša člana porodice (za nerezidente).</summary>
    [StringLength(50)]
    public string? PassportNumber { get; set; }

    /// <summary>FBA ID člana porodice (za nerezidente).</summary>
    [StringLength(50)]
    public string? FBAId { get; set; }

    #endregion

    #region Odnos i porodično stablo

    /// <summary>Vrsta odnosa ovog člana prema matičnom licu (<see cref="RelatedPerson"/>) odn. prema referentnom članu (<see cref="ParentFamilyMember"/>), ako je postavljen.</summary>
    [Required]
    public FamilyRelationshipType RelationshipType { get; set; }

    /// <summary>
    /// Opciona referenca na drugog člana porodice unutar iste porodice, kroz kojeg je
    /// ovaj član povezan sa matičnim licem (npr. dijete bračnog partnera).
    /// Null znači da je član direktno povezan sa matičnim licem (<see cref="RelatedPerson"/>).
    /// Omogućava hijerarhijski prikaz porodičnog stabla bez promjene šeme.
    /// </summary>
    public Guid? ParentFamilyMemberId { get; set; }

    public virtual FamilyMember? ParentFamilyMember { get; set; }

    /// <summary>Djeca/potomci ovog člana porodice u stablu (obrnuta veza za <see cref="ParentFamilyMemberId"/>).</summary>
    public virtual ICollection<FamilyMember> ChildFamilyMembers { get; set; } = new List<FamilyMember>();

    #endregion

    #region BaseEntity polja (audit / soft delete)

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? ModifiedAt { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    #endregion
}
