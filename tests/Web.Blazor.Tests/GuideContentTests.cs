using System.Text.Json;
using RBBH.RelatedPartiesRegister.Web.Api;
using RBBH.RelatedPartiesRegister.Web.Components.Registry;

namespace Web.Blazor.Tests;

public sealed class GuideContentTests
{
    [Theory]
    [InlineData("FL_VIEW", "physical")]
    [InlineData("PL_VIEW", "legal")]
    [InlineData("LIMITI_VIEW", "limits")]
    [InlineData("KAPITAL_VIEW", "limits")]
    public void SectionsFollowActualPermissionsRatherThanRoleNames(string permission, string section)
    {
        var visible = GuideContent.Visible(new("u", "Name", "user", "u@example.test", ["gost"], [permission]));
        Assert.Contains(visible, item => item.Id == section); Assert.Contains(visible, item => item.Id == "reports"); Assert.DoesNotContain(visible, item => item.Administrator);
    }
    [Fact]
    public void ReportingReadRemainsAvailableWithoutMutationPermissionsAndAdminNeedsRealApplicationAccess()
    {
        Assert.Empty(GuideContent.Visible(new("u", null, null, null, [], [])));
        Assert.Equal("reports", Assert.Single(GuideContent.Visible(new("u", null, null, null, ["gost"], []))).Id);
        Assert.DoesNotContain(GuideContent.Visible(new("u", null, null, null, ["admin"], [])), item => item.Administrator);
        Assert.Contains(GuideContent.Visible(new("u", null, null, null, ["admin"], ["ADMINISTRATION_MANAGE"])), item => item.Administrator);
    }
    [Theory]
    [InlineData("bs")]
    [InlineData("en")]
    public void EveryGuideSectionAndPointHasOwnedResourcesInBothBusinessLanguages(string culture)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RelatedPartiesRegister.slnx"))) directory = directory.Parent;
        Assert.NotNull(directory);
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory.FullName, "Frontend", "Localization", culture + ".json")));
        var resource = document.RootElement.GetProperty("translations");
        var all = GuideContent.Visible(new("u", null, null, null, ["admin"], ["FL_VIEW", "PL_VIEW", "LIMITI_VIEW", "KAPITAL_VIEW", "ADMINISTRATION_MANAGE"]));
        Assert.Equal(5, all.Count);
        foreach (var section in all)
        {
            Assert.False(string.IsNullOrWhiteSpace(resource.GetProperty($"guide.{section.Id}.title").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(resource.GetProperty($"guide.{section.Id}.summary").GetString()));
            for (var point = 1; point <= section.Points; point++) Assert.False(string.IsNullOrWhiteSpace(resource.GetProperty($"guide.{section.Id}.point{point}").GetString()));
        }
    }
}
