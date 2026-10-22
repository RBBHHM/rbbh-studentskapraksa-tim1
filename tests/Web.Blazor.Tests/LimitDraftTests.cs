using RBBH.RelatedPartiesRegister.Web.Components.Registry;

namespace Web.Blazor.Tests;

public sealed class LimitDraftTests
{
    [Fact]
    public void OptionalAmountsAndDateRemainNullAndZeroIsNotDropped()
    {
        var model = new LimitDraft { LegalEntityId = Guid.NewGuid(), TipLimita = "REG", OdobreniLimit = 0 };
        Assert.Empty(model.Validate()); var request = model.ToRequest();
        Assert.Equal(0m, request.OdobreniLimit); Assert.Null(request.MaksimalnaOcekivanaUtilizacija); Assert.Null(request.RokOcekivaneUtilizacije);
    }
    [Fact]
    public void MissingEntityAndTypePreventRequestCreation()
    {
        var model = new LimitDraft(); Assert.Contains("LegalEntityId", model.Validate().Keys); Assert.Contains("TipLimita", model.Validate().Keys);
        Assert.Throws<InvalidOperationException>(() => model.ToRequest());
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NegativeAmountsAreFieldSpecific(bool approved)
    {
        var model = new LimitDraft { LegalEntityId = Guid.NewGuid(), TipLimita = "REG", OdobreniLimit = approved ? -1 : 1, MaksimalnaOcekivanaUtilizacija = approved ? 1 : -1 };
        Assert.Contains(approved ? "OdobreniLimit" : "MaksimalnaOcekivanaUtilizacija", model.Validate().Keys);
    }
}
