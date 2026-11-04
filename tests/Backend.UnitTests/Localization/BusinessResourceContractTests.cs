using System.Text.RegularExpressions;
using RBBH.ConnectedParties.Exceptions;
using RBBH.ConnectedParties.Localization;

namespace UnitTests.Localization;

public sealed class BusinessResourceContractTests
{
    [Fact]
    public void SafeFallbacksHaveSemanticKeysAndExplicitBusinessMessages()
    {
        Assert.NotEmpty(BackendMessages.All);
        foreach (var (key, label) in BackendMessages.All)
        {
            Assert.Matches("^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)+$", key);
            Assert.False(string.IsNullOrWhiteSpace(label)); Assert.NotEqual(key, label);
            Assert.InRange(label.Length, 1, 4000);
        }
        var response = new BusinessRuleException(400, "period.invalid_response", new object?[] { 10, 490 });
        Assert.Contains("10", response.Message); Assert.Contains("490", response.Message);
        Assert.DoesNotContain("{0}", response.Message); Assert.Equal(2, response.Parameters.Count);
        var unknown = new BusinessRuleException(400, "unmapped.internal.code");
        Assert.Equal(BackendMessages.Text("request.invalid.detail"), unknown.Message);
        Assert.DoesNotContain("unmapped.internal.code", unknown.Message);
    }

    [Fact]
    public void BusinessRuleCallSitesUseResourcesInsteadOfEmbeddedHumanMessages()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "Backend"))) root = root.Parent;
        Assert.NotNull(root);
        var violations = new List<string>();
        var call = new Regex("BusinessRuleException\\s*\\(\\s*[^,()\\r\\n]+,\\s*[^,()\\r\\n]+,\\s*[$@]*\"");
        var helper = new Regex("BusinessRuleException[^\\r\\n]*=>\\s*new\\(\\s*[^,()\\r\\n]+,\\s*[^,()\\r\\n]+,\\s*[$@]*\"");
        Assert.Matches(call, "new BusinessRuleException(400, \"example.invalid\", \"Embedded message\")");
        Assert.Matches(helper, "BusinessRuleException Invalid() => new(400, \"example.invalid\", \"Embedded message\")");
        Assert.DoesNotMatch(call, "new BusinessRuleException(400, \"example.invalid\"); Next(1, \"data\")");
        var codes = new Regex("(?:BusinessRuleException|new)\\s*\\(\\s*\\d+\\s*,\\s*\"([a-z][a-z0-9_.]+)\"");
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root!.FullName, "Backend"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "Migrations" + Path.DirectorySeparatorChar) &&
                !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)))
        {
            var source = File.ReadAllText(file);
            if (call.IsMatch(source) || helper.IsMatch(source)) violations.Add(Path.GetRelativePath(root.FullName, file));
            foreach (Match match in codes.Matches(source))
                Assert.True(BackendMessages.All.ContainsKey("errors." + match.Groups[1].Value.Replace('_', '.')),
                    $"Missing safe fallback for {match.Groups[1].Value} in {file}");
        }
        Assert.Empty(violations);
    }
    [Fact]
    public void ControllerAndExplicitDtoMessagesUseResourceKeys()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "Backend"))) root = root.Parent;
        Assert.NotNull(root);
        var violations = new List<string>();
        var direct = new Regex(@"\b(?:Title|Detail|message|error|ErrorMessage)\s*=\s*[$@]*""[^""]");
        var key = new Regex("BackendMessages\\.Message\\(\\s*\"([a-z][a-z0-9.]+)\"\\s*[,)]");
        foreach (var folder in new[] { "API/Controllers", "DL/DTO" })
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root!.FullName, "Backend", folder), "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            if (direct.IsMatch(source)) violations.Add(Path.GetRelativePath(root.FullName, file));
            foreach (Match match in key.Matches(source)) Assert.Contains(match.Groups[1].Value, BackendMessages.All.Keys);
        }
        Assert.Empty(violations);
    }

    [Fact]
    public void ServiceValidationAndImportMessagesDoNotEmbedUserFacingText()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "Backend"))) root = root.Parent;
        Assert.NotNull(root);
        var violations = new List<string>();
        var direct = new Regex(@"(?:ValidationError|NotFoundError)\s*\(\s*[$@]*""|ValidationException\s*\(\s*(?:""[^""]*""\s*,\s*)?[$@]*""[^""]*""\s*\)");
        var key = new Regex("BackendMessages\\.Message\\(\\s*\"([a-z][a-z0-9.]+)\"\\s*[,)]");
        foreach (var folder in new[] { "BL/Services", "Helpers/Validators", "Helpers/Excel" })
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root!.FullName, "Backend", folder), "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            if (direct.IsMatch(source)) violations.Add(Path.GetRelativePath(root.FullName, file));
            foreach (Match match in key.Matches(source)) Assert.Contains(match.Groups[1].Value, BackendMessages.All.Keys);
        }
        Assert.Empty(violations);
    }

}
