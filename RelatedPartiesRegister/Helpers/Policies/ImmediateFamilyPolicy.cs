using RBBH.ConnectedParties.DL.DTO.RelatedPersons;
using RBBH.ConnectedParties.Helpers.Validators;

namespace RBBH.ConnectedParties.Helpers.Policies;

/// <summary>
/// Jedino mjesto na kojem se primjenjuju obavezne vrijednosti za člana uže porodice.
/// Pravilo se izvršava na serveru pa ga nije moguće zaobići izmijenjenim HTTP zahtjevom.
/// </summary>
public static class ImmediateFamilyPolicy
{
    public static void Apply(CreateRelatedPersonDTO dto) => Apply(
        dto.SpecialRelationBasis,
        clearLink: () => { dto.RelatedToPersonId = null; dto.FamilyRelationshipType = null; },
        applyDefaults: () =>
        {
            dto.IsIdentifiedStaff = false;
            dto.ConnectedWithBank = true;
            dto.SpecialRelationshipWithBank = false;
            dto.SpecialContract = false;
            dto.MalusClawback = false;
            dto.DeclarationNoFamilyMembers = true;
        });

    public static void Apply(UpdateRelatedPersonDTO dto) => Apply(
        dto.SpecialRelationBasis,
        clearLink: () => { dto.RelatedToPersonId = null; dto.FamilyRelationshipType = null; },
        applyDefaults: () =>
        {
            dto.IsIdentifiedStaff = false;
            dto.ConnectedWithBank = true;
            dto.SpecialRelationshipWithBank = false;
            dto.SpecialContract = false;
            dto.MalusClawback = false;
            dto.DeclarationNoFamilyMembers = true;
        });

    private static void Apply(string? specialRelationBasis, Action clearLink, Action applyDefaults)
    {
        if (RelatedPersonValidator.IsImmediateFamily(specialRelationBasis))
            applyDefaults();
        else
            clearLink();
    }
}
