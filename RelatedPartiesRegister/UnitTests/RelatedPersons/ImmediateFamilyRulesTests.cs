using FluentAssertions;
using RBBH.ConnectedParties.DL.DTO.RelatedPersons;
using RBBH.ConnectedParties.DL.Entities.RelatedPersons;
using RBBH.ConnectedParties.Helpers.Validators;
using RBBH.ConnectedParties.Helpers.Policies;

namespace UnitTests.RelatedPersons;

public class ImmediateFamilyRulesTests
{
    [Theory]
    [InlineData("UZA_PORODICA")]
    [InlineData("Član uže porodice povezanog lica")]
    public void IsImmediateFamily_PrepoznajeSifruIIme(string value)
    {
        RelatedPersonValidator.IsImmediateFamily(value).Should().BeTrue();
    }

    [Fact]
    public void Validate_ClanUzePorodiceBezPovezanogLica_VracaJasnuGresku()
    {
        var dto = ValidDto();
        dto.SpecialRelationBasis = "UZA_PORODICA";
        dto.RelatedToPersonId = null;
        dto.FamilyRelationshipType = FamilyRelationshipType.Spouse;

        RelatedPersonValidator.Validate(dto)
            .Should().Be("Za člana uže porodice odaberite fizičko lice s kojim je povezan.");
    }

    [Fact]
    public void Validate_ClanUzePorodiceBezVrsteOdnosa_VracaJasnuGresku()
    {
        var dto = ValidDto();
        dto.SpecialRelationBasis = "UZA_PORODICA";
        dto.RelatedToPersonId = Guid.NewGuid();
        dto.FamilyRelationshipType = null;

        RelatedPersonValidator.Validate(dto)
            .Should().Be("Za člana uže porodice odaberite vrstu porodičnog odnosa.");
    }

    [Fact]
    public void Validate_NezaposlenoLiceKojeNijePorodica_JeOdbijeno()
    {
        var dto = ValidDto();
        dto.IsIdentifiedStaff = false;

        RelatedPersonValidator.Validate(dto).Should().Contain("mora biti identifikovani zaposlenik");
    }

    [Fact]
    public void Policy_ClanUzePorodice_PostavljaSveObavezneVrijednosti()
    {
        var dto = ValidDto();
        dto.SpecialRelationBasis = "UZA_PORODICA";
        dto.RelatedToPersonId = Guid.NewGuid();
        dto.FamilyRelationshipType = FamilyRelationshipType.Child;
        dto.IsIdentifiedStaff = true;
        dto.ConnectedWithBank = false;
        dto.SpecialRelationshipWithBank = true;
        dto.SpecialContract = true;
        dto.MalusClawback = true;
        dto.DeclarationNoFamilyMembers = false;

        ImmediateFamilyPolicy.Apply(dto);

        dto.IsIdentifiedStaff.Should().BeFalse();
        dto.ConnectedWithBank.Should().BeTrue();
        dto.SpecialRelationshipWithBank.Should().BeFalse();
        dto.SpecialContract.Should().BeFalse();
        dto.MalusClawback.Should().BeFalse();
        dto.DeclarationNoFamilyMembers.Should().BeTrue();
        dto.RelatedToPersonId.Should().NotBeNull();
        dto.FamilyRelationshipType.Should().Be(FamilyRelationshipType.Child);
    }

    [Fact]
    public void Policy_DrugiOsnov_UklanjaZaostaluPorodicnuVezu()
    {
        var dto = ValidDto();
        dto.RelatedToPersonId = Guid.NewGuid();
        dto.FamilyRelationshipType = FamilyRelationshipType.Spouse;

        ImmediateFamilyPolicy.Apply(dto);

        dto.RelatedToPersonId.Should().BeNull();
        dto.FamilyRelationshipType.Should().BeNull();
    }

    private static CreateRelatedPersonDTO ValidDto() => new()
    {
        FirstName = "Hana",
        LastName = "Mahmutović",
        Residency = ResidencyType.Resident,
        JMBG = "2801984175000",
        GCCNumber = "1001",
        GCCName = "RBI GCC",
        RelationBasis = "ZOB-2-V-5",
        RelationDescription = "Povezano fizičko lice",
        SpecialRelationBasis = "UPRAVA",
        IsIdentifiedStaff = true,
        DateFrom = new DateTime(2026, 1, 1),
        DateTo = new DateTime(2027, 1, 1)
    };
}
