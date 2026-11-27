using RBBH.RelatedPartiesRegister.Web.Components.Registry;

namespace Web.Blazor.Tests;

public sealed class AuditChangeTests
{
    [Fact]
    public void ComparesSnapshotsPerField()
    {
        var changes = AuditChange.Compare("""{"a":"1","b":"2","c":"3"}""", """{"a":"1","b":"9","d":"4"}""").ToDictionary(item => item.Field);
        Assert.Equal(AuditChangeKind.Unchanged, changes["a"].Kind);
        Assert.Equal(AuditChangeKind.Changed, changes["b"].Kind);
        Assert.Equal(AuditChangeKind.Removed, changes["c"].Kind);
        Assert.Equal(AuditChangeKind.Added, changes["d"].Kind);
    }

    [Fact]
    public void FlattensNestedSnapshotsAndFormatsReferenceNamesWithoutTreatingPermissionOrderAsAChange()
    {
        var changes = AuditChange.Compare(
            """{"Definition":{"Name":"Srodstvo","IsActive":true},"PermissionIds":[14,9],"LegalEntityId":42}""",
            """{"Definition":{"Name":"Srodstvo","IsActive":false},"PermissionIds":[9,14],"LegalEntityId":43,"Columns":[{"Name":"Kod"}]}""",
            new Dictionary<long, string> { [9] = "LIMITI_VIEW", [14] = "PL_VIEW" }, yes: "Da", no: "Ne",
            referenceNames: new Dictionary<string, Dictionary<long, string>> { ["LegalEntityId"] = new() { [42] = "Firma A", [43] = "Firma B" } })
            .ToDictionary(change => change.Field);

        Assert.Equal(new AuditFieldChange("Definition.IsActive", "Da", "Ne", AuditChangeKind.Changed), changes["Definition.IsActive"]);
        Assert.Equal(new AuditFieldChange("LegalEntityId", "Firma A", "Firma B", AuditChangeKind.Changed), changes["LegalEntityId"]);
        Assert.Equal("Kod", changes["Columns [1].Name"].New);
        Assert.Equal(AuditChangeKind.Unchanged, changes["PermissionIds"].Kind);
        Assert.Contains("LIMITI_VIEW", changes["PermissionIds"].Old);
        Assert.DoesNotContain("[", changes["PermissionIds"].Old);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    public void UnparseableSnapshotsYieldNoRows(string value) => Assert.Empty(AuditChange.Compare(value, null));

    [Theory]
    [InlineData("CREATE", "success")]
    [InlineData("ROLE_CATALOG_CREATE", "success")]
    [InlineData("ROLE_CATALOG_UPDATE", "warning")]
    [InlineData("update", "warning")]
    [InlineData("DELETE", "danger")]
    [InlineData("OTHER", "neutral")]
    public void ActionToneFollowsTheChangeType(string action, string tone) => Assert.Equal(tone, AuditChange.Tone(action));
}
