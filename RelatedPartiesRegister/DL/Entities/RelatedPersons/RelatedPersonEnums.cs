namespace RBBH.ConnectedParties.DL.Entities.RelatedPersons;

/// <summary>
/// Tip rezidentnosti fizičkog lica.
/// </summary>
public enum ResidencyType
{
    /// <summary>Rezident — identifikacija putem JMBG-a.</summary>
    Resident = 1,

    /// <summary>Nerezident — identifikacija putem broja pasoša i/ili FBA ID-a.</summary>
    NonResident = 2
}

/// <summary>
/// Status zapisa povezanog fizičkog lica kroz radni proces (workflow).
/// Za ovaj sprint se koristi samo <see cref="Draft"/>, ali se ostali statusi
/// definišu unaprijed kako bi se izbjegla kasnija promjena modela / migracija.
/// </summary>
public enum RelatedPersonStatus
{
    /// <summary>Zapis je kreiran i još uvijek u izradi (nije proslijeđen na obradu/odobrenje).</summary>
    Draft = 1,

    /// <summary>Zapis je poslan na verifikaciju/odobrenje.</summary>
    Submitted = 2,

     /// <summary>Zapis je verificiran.</summary>
     Verified = 3,

    /// <summary>Zapis je odbijen i vraćen na doradu.</summary>
    Rejected = 4,

    /// <summary>Zapis je arhiviran/deaktiviran.</summary>
    Archived = 5,

    /// <summary>Zapis je odobren/aktivan.</summary>
    Approved = 6
}

/// <summary>
/// Vrsta srodstva/odnosa člana porodice prema matičnom (povezanom) fizičkom licu.
/// Definisano kao enum za ovaj sprint; po potrebi se kasnije može zamijeniti
/// šifrarnikom (CodeList) bez promjene strukture FamilyMember tabele
/// (kolona RelationshipType ostaje, samo se mapiranje vrijednosti proširi).
/// </summary>
public enum FamilyRelationshipType
{
    Spouse = 1,
    Partner = 2,
    Parent = 3,
    Child = 4,
    Sibling = 5,
    StepParent = 6,
    StepChild = 7,
    Guardian = 8,
    Other = 99
}
