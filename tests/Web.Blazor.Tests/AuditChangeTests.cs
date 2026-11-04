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

    [Theory]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    public void UnparseableSnapshotsYieldNoRows(string value) => Assert.Empty(AuditChange.Compare(value, null));

    [Theory]
    [InlineData("CREATE", "success")]
    [InlineData("update", "warning")]
    [InlineData("DELETE", "danger")]
    [InlineData("OTHER", "neutral")]
    public void ActionToneFollowsTheChangeType(string action, string tone) => Assert.Equal(tone, AuditChange.Tone(action));
}
