using System.Text.Json;
using RBBH.RelatedPartiesRegister.Web.Api;

namespace Web.Blazor.Tests;

public sealed class RegistrySearchTests
{
    [Theory]
    [InlineData("physical")]
    [InlineData("legal")]
    [InlineData("limits")]
    public void ScalarSearchTrimsUserWhitespaceAsTheExistingResourcePageDoes(string resource)
    {
        var json = "{\"firstName\":\"Fixture\",\"name\":\"Fixture\",\"naziv\":\"Fixture\"}";
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Func<string?, bool> matches = resource switch
        {
            "physical" => JsonSerializer.Deserialize<RelatedPerson>(json, options)!.Matches,
            "legal" => JsonSerializer.Deserialize<LegalEntity>(json, options)!.Matches,
            "limits" => JsonSerializer.Deserialize<LimitRecord>(json, options)!.Matches,
            _ => throw new ArgumentException("Unknown test resource.")
        };
        Assert.True(matches("  FIXTURE  ")); Assert.True(matches("   ")); Assert.False(matches("  unmatched  "));
    }
}
