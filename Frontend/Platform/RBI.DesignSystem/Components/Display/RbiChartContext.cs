using System.Text;
using System.Text.RegularExpressions;

namespace RBI.DesignSystem.Components.Display;

// Internal cascading presentation, deliberately not exposed as a consumer state/context API.
internal sealed class RbiChartContext
{
    private readonly IReadOnlyDictionary<string, RbiChartSeries> _byKey;
    internal RbiChartContext(string id, string tooltipId, IReadOnlyList<RbiChartSeries> series)
    {
        Id = id;
        TooltipId = tooltipId;
        Series = Array.AsReadOnly(series.ToArray());
        _byKey = Series.ToDictionary(item => item.Key, StringComparer.Ordinal);
    }
    /// <summary>Stable owned chart identity.</summary>
    internal string Id { get; }
    /// <summary>Stable tooltip identity for consumer-owned SVG focus targets.</summary>
    internal string TooltipId { get; }
    /// <summary>Immutable presentation configuration.</summary>
    internal IReadOnlyList<RbiChartSeries> Series { get; }
    /// <summary>Resolves an optional series presentation without untyped payload traversal.</summary>
    internal RbiChartSeries? GetSeries(string key) => _byKey.GetValueOrDefault(key);
    /// <summary>Safe scoped color variable, with currentColor fallback.</summary>
    internal string GetColor(string key) => _byKey.ContainsKey(key) ? $"var(--color-{key}, currentColor)" : "currentColor";

    internal string ColorRules()
    {
        var light = new StringBuilder();
        var dark = new StringBuilder();
        foreach (var item in Series)
        {
            if (item.Color is not null) light.Append($"--color-{item.Key}:{item.Color};");
            if (item.DarkColor is not null) dark.Append($"--color-{item.Key}:{item.DarkColor};");
        }
        var selector = $"[data-rbi-chart=\"{Id}\"]";
        return (light.Length > 0 ? $"{selector}{{{light}}}" : "") + (dark.Length > 0 ? $"[data-theme=\"dark\"] {selector},.dark {selector}{{{dark}}}" : "");
    }
}

internal static partial class RbiChartPresentation
{
    [GeneratedRegex("^[A-Za-z][A-Za-z0-9_-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex Identifier();
    [GeneratedRegex("^[A-Za-z0-9#%,.() _+/-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorCharacters();
    internal static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || !Identifier().IsMatch(key)) throw new ArgumentException("Chart series keys must be safe CSS identifiers.");
    }
    internal static void ValidateColor(string? value)
    {
        if (value is not null && (string.IsNullOrWhiteSpace(value) || value.Length > 256 || !ColorCharacters().IsMatch(value) || value.Contains("/*", StringComparison.Ordinal) || value.Contains("url(", StringComparison.OrdinalIgnoreCase) || value.Contains("expression(", StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Chart colors must be CSS color expressions without declarations, URLs or markup.");
    }
}
