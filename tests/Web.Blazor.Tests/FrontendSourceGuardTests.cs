using System.Text.Json;
using System.Text.RegularExpressions;

namespace Web.Blazor.Tests;

/// <summary>Build-time guards: no literal visible text, no literal business options, no unknown resource keys.</summary>
public sealed class FrontendSourceGuardTests
{
    private static readonly string Frontend = FindFrontend();

    // Business option lists still in source. Each entry is tracked debt: remove it when the list is served by a code list.
    private static readonly HashSet<string> KnownHardcodedBusinessLists =
    [
        "Components/Pages/Period.razor|Statuses",
        "Components/Pages/Guide.razor|States",
    ];

    private static string FindFrontend()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RelatedPartiesRegister.slnx"))) directory = directory.Parent;
        return Path.Combine(directory!.FullName, "Frontend");
    }

    private static IEnumerable<(string Path, string Text)> Sources(string pattern) =>
        Directory.EnumerateFiles(Path.Combine(Frontend, "Components"), pattern, SearchOption.AllDirectories)
            .Select(file => (Path.GetRelativePath(Frontend, file).Replace(Path.DirectorySeparatorChar, '/'), File.ReadAllText(file)));

    private static string Markup(string razor)
    {
        razor = Regex.Replace(razor, @"@\*.*?\*@", " ", RegexOptions.Singleline);
        razor = Regex.Replace(razor, @"<!--.*?-->", " ", RegexOptions.Singleline);
        razor = Regex.Replace(razor, @"<(script|style)\b.*?</\1>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var code = razor.IndexOf("@code", StringComparison.Ordinal);
        return code < 0 ? razor : razor[..code];
    }

    [Fact]
    public void MarkupContainsNoLiteralVisibleText()
    {
        var textNode = new Regex(@">([^<>@{}]*\p{L}{2,}[^<>@{}]*)<");
        var attribute = new Regex(@"\b(title|placeholder|aria-label|alt|AccessibleLabel|Label|Title|Description|Eyebrow|Text|Message)=""([^""@]*\p{L}{3,}[^""@]*)""");
        var found = new List<string>();
        foreach (var (path, text) in Sources("*.razor"))
        {
            var markup = Markup(text);
            found.AddRange(textNode.Matches(markup).Select(m => $"{path}: text '{m.Groups[1].Value.Trim()}'").Where(item => !item.EndsWith("''")));
            found.AddRange(attribute.Matches(markup).Where(m => !Regex.IsMatch(m.Groups[2].Value, @"^[\w\-./#:]+$")).Select(m => $"{path}: {m.Groups[1].Value}='{m.Groups[2].Value}'"));
        }
        Assert.True(found.Count == 0, "Use localization resource keys instead of literal text:\n" + string.Join('\n', found));
    }

    [Fact]
    public void FormsDoNotRepeatFieldMessagesInAValidationSummary()
    {
        var offenders = Sources("*.razor").Where(source => Markup(source.Text).Contains("<ValidationSummary", StringComparison.Ordinal)).Select(source => source.Path).ToArray();
        Assert.True(offenders.Length == 0, "Use <FormSummary> (form-level messages only):" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void MarkupHasNoLiteralSelectOptionsAndNoNewHardcodedBusinessLists()
    {
        var literalOption = new Regex(@"<option\b[^>]*\bvalue=""([^""@][^""]*)""");
        var staticList = new Regex(@"static readonly (?:string|int)\[\]\s+(\w+)\s*=\s*\[\s*""");
        var found = new List<string>();
        foreach (var (path, text) in Sources("*.razor"))
        {
            found.AddRange(literalOption.Matches(Markup(text)).Select(m => $"{path}: <option value='{m.Groups[1].Value}'>"));
            foreach (Match match in staticList.Matches(text))
                if (!KnownHardcodedBusinessLists.Contains($"{path}|{match.Groups[1].Value}") && !match.Groups[1].Value.EndsWith("Keys", StringComparison.Ordinal) && !match.Groups[1].Value.StartsWith("Columns", StringComparison.Ordinal))
                    found.Add($"{path}: static list '{match.Groups[1].Value}' - source business options from a code list");
        }
        Assert.True(found.Count == 0, string.Join('\n', found));
    }

    [Fact]
    public void EveryLiteralResourceKeyExistsInAllThreeCatalogs()
    {
        var keys = new List<HashSet<string>>();
        foreach (var culture in new[] { "bs", "en", "de" })
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Frontend, "Localization", culture + ".json")));
            keys.Add(document.RootElement.GetProperty("translations").EnumerateObject().Select(item => item.Name).ToHashSet());
        }
        var used = new Regex(@"\bT\[""([^""]+)""\]|\bT\.Format\(""([^""]+)""");
        var missing = Sources("*.razor").Concat(Sources("*.cs"))
            .SelectMany(source => used.Matches(source.Text).Select(m => (source.Path, Key: m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)))
            .Where(item => keys.Any(set => !set.Contains(item.Key)))
            .Select(item => $"{item.Path}: {item.Key}").Distinct().ToArray();
        Assert.True(missing.Length == 0, "Missing resource keys:\n" + string.Join('\n', missing));
    }
}
