using RBBH.RelatedPartiesRegister.Web.Api;
using RBBH.RelatedPartiesRegister.Web.Components.Registry;

namespace Web.Blazor.Tests;

public sealed class LegalEntityDraftTests
{
    private static LegalEntityDraft Valid() => new()
    {
        Name = "Company",
        IsResident = false,
        FbaId = "123456",
        MaticniBroj = "12345",
        BasisOfConnection = "ZOB-2-V-5",
        ConnectionDescription = "Relationship",
        ConnectedWithBank = false,
        DateFrom = new(2026, 1, 1)
    };
    [Fact]
    public void ExplicitFalseAndOptionalEndDateAreValid()
    {
        var model = Valid(); Assert.Empty(model.Validate());
        Assert.False(model.ToRequest().ConnectedWithBank); Assert.Null(model.ToRequest().DateTo);
    }
    [Theory]
    [InlineData("1234567890125", true)]
    [InlineData("1234567890123", false)]
    [InlineData("123", false)]
    [InlineData("123456789012X", false)]
    public void TaxChecksumUsesExistingBackendContract(string number, bool valid) => Assert.Equal(valid, LegalEntityDraft.ValidTaxNumber(number));
    [Fact]
    public void MissingChoicesAndReversedDatesHaveFieldErrors()
    {
        var model = Valid(); model.IsResident = null; model.ConnectedWithBank = null; model.DateTo = model.DateFrom!.Value.AddDays(-1);
        var errors = model.Validate(); Assert.Contains("IsResident", errors.Keys); Assert.Contains("ConnectedWithBank", errors.Keys); Assert.Contains("DateTo", errors.Keys);
        Assert.Throws<InvalidOperationException>(() => model.ToRequest());
    }
    [Fact]
    public void ResidencySwitchClearsOnlyInactiveIdentifier()
    {
        var model = Valid(); model.IsResident = true; model.TaxNumber = "1234567890125"; model.ResidencyChanged();
        Assert.Null(model.FbaId); Assert.Equal("12345", model.ToRequest().MaticniBroj);
        model.IsResident = false; model.ResidencyChanged(); Assert.Null(model.TaxNumber);
    }
    [Fact]
    public void UpdatePreservesDistinctUneditableMatbrojWithoutExposingStatusMutation()
    {
        var entity = new LegalEntity(Guid.NewGuid(), false, null, "12345", "123456", "Company", "SERVER-ONLY",
            "ZOB-2-V-5", "Relationship", false, new(2026, 1, 1), null, "Verified", "author", DateTime.UtcNow, null, null, "verifier", DateTime.UtcNow);
        var draft = LegalEntityDraft.From(entity); draft.Name = "Renamed"; draft.MaticniBroj = "98765";
        var request = draft.ToRequest(); Assert.Equal("SERVER-ONLY", request.Matbroj); Assert.Equal("98765", request.MaticniBroj);
        Assert.Null(typeof(LegalEntityRequest).GetProperty("Status"));
        Assert.True(entity.Matches("verifier")); Assert.True(entity.Matches("SERVER-ONLY")); Assert.False(entity.Matches("unmatched"));
    }
    [Theory]
    [InlineData("AB123")]
    [InlineData("123.4")]
    [InlineData("123456789012345678901234567890123456789012345678901")]
    public void RegistrationValidationIsPreserved(string registration)
    {
        var model = Valid(); model.MaticniBroj = registration; Assert.Contains("MaticniBroj", model.Validate().Keys);
    }
}
