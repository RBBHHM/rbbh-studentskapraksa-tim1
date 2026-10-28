using RBBH.RelatedPartiesRegister.Web.Components.Registry;

namespace Web.Blazor.Tests;

public sealed class PhysicalPersonDraftTests
{
    public static PhysicalPersonDraft Valid() => new()
    {
        FirstName = "Test", LastName = "Osoba", Residency = 2, PassportNumber = "AB12345", FbaId = "12345",
        RelationBasis = "ZOB-2-V-5", RelationDescription = "Business relationship", SpecialRelationBasis = "UPRAVA",
        DateFrom = new(2026, 1, 1), DateTo = new(2026, 12, 31), IsIdentifiedStaff = true,
        DeclarationNoFamilyMembers = false, ConnectedWithBank = true, SpecialRelationshipWithBank = false,
        SpecialContract = false, MalusClawback = false
    };

    [Fact]
    public void ValidNonresidentKeepsAllExplicitFalseChoicesInWireRequest()
    {
        var draft = Valid();
        Assert.Empty(draft.Validate());
        var request = draft.ToRequest();
        Assert.False(request.DeclarationNoFamilyMembers);
        Assert.False(request.MalusClawback);
        Assert.Equal("12345", request.FbaId);
        Assert.Null(request.Jmbg);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Test9")]
    [InlineData("")]
    public void InvalidNamesAreFieldSpecific(string name)
    {
        var draft = Valid(); draft.FirstName = name;
        Assert.Contains(nameof(draft.FirstName), draft.Validate(true).Keys);
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("A 1234")]
    [InlineData("<script>")]
    public void PassportValidationIsPreserved(string passport)
    {
        var draft = Valid(); draft.PassportNumber = passport;
        Assert.Contains(nameof(draft.PassportNumber), draft.Validate(true).Keys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345678901")]
    [InlineData("12x")]
    public void NonresidentRequiresNumericFba(string fba)
    {
        var draft = Valid(); draft.FbaId = fba;
        Assert.Contains(nameof(draft.FbaId), draft.Validate(true).Keys);
    }

    [Theory]
    [InlineData("0101990500003", true)]
    [InlineData("0101990500000", false)]
    [InlineData("3102990500009", false)]
    [InlineData("abcdefghijklm", false)]
    public void NationalIdChecksDateAndChecksum(string jmbg, bool expected) => Assert.Equal(expected, PhysicalPersonDraft.ValidJmbg(jmbg));

    [Fact]
    public void ResidencySwitchClearsIncompatibleIdentityFields()
    {
        var draft = Valid(); draft.Residency = 1; draft.ResidencyChanged();
        Assert.Null(draft.PassportNumber); Assert.Null(draft.FbaId);
        Assert.Contains(nameof(draft.Jmbg), draft.Validate(true).Keys);
        draft.Jmbg = "0101990500003"; draft.Residency = 2; draft.ResidencyChanged();
        Assert.Null(draft.Jmbg);
    }

    [Fact]
    public void FamilySelectionAppliesExistingLockedDefaultsAndRequiresParent()
    {
        var draft = Valid(); draft.SpecialRelationBasis = "UZA_PORODICA"; draft.SpecialBasisChanged();
        Assert.False(draft.IsIdentifiedStaff); Assert.True(draft.ConnectedWithBank);
        Assert.False(draft.SpecialRelationshipWithBank); Assert.True(draft.DeclarationNoFamilyMembers);
        Assert.Contains(nameof(draft.RelatedToPersonId), draft.Validate().Keys);
        draft.RelatedToPersonId = Guid.NewGuid(); Assert.Empty(draft.Validate());
        Assert.Equal(draft.RelatedToPersonId, draft.ToRequest().RelatedToPersonId);
    }

    [Fact]
    public void LeavingFamilyClearsParentAndRequiresNewExplicitBooleanChoices()
    {
        var draft = Valid(); draft.RelatedToPersonId = Guid.NewGuid();
        draft.SpecialRelationBasis = "UPRAVA"; draft.SpecialBasisChanged();
        Assert.Null(draft.RelatedToPersonId); Assert.Null(draft.ConnectedWithBank);
        Assert.Contains(nameof(draft.ConnectedWithBank), draft.Validate().Keys);
        Assert.Throws<InvalidOperationException>(() => draft.ToRequest());
    }

    [Fact]
    public void BusinessErrorsDoNotBlockIdentityStepButBlockFinalSave()
    {
        var draft = Valid(); draft.RelationBasis = null; draft.DateTo = new(2025, 1, 1);
        Assert.Empty(draft.Validate(true));
        Assert.Contains(nameof(draft.RelationBasis), draft.Validate().Keys);
        Assert.Contains(nameof(draft.DateTo), draft.Validate().Keys);
    }
}
