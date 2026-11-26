using RBBH.RelatedPartiesRegister.Web.Components.Registry;

namespace Web.Blazor.Tests;

public sealed class CapitalDraftTests
{
    [Fact]
    public void EmptySingletonDoesNotPopulateZeroAmountsOrInventDate()
    {
        var model = CapitalDraft.From(null); Assert.Null(model.CoreCapital); Assert.Null(model.ValidTill);
        Assert.Equal(4, model.Validate().Count); Assert.Throws<InvalidOperationException>(() => model.ToRequest());
    }
    [Fact]
    public void ExplicitZerosAreValidAndNonNegativeRuleIsFieldSpecific()
    {
        var model = new CapitalDraft { CoreCapital = 0, RegulatoryCapital = 0, SupplementaryCapital = 0, ValidTill = DateTime.UtcNow.Date.AddDays(2) };
        Assert.Empty(model.Validate()); Assert.Equal(0m, model.ToRequest().RegulatoryCapital);
        model.CoreCapital = -1; Assert.Contains("CoreCapital", model.Validate().Keys);
    }
}
