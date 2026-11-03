using System.Collections.Frozen;
using System.Text.Json;

namespace RBBH.ConnectedParties.Localization;

/// <summary>
/// Immutable safe BS fallback when exception construction precedes scoped runtime lookup.
/// Runtime Language Packs remain authoritative; GlobalExceptionHandler resolves the same key.
/// </summary>
public static class BackendErrorFallbacks
{
    private static readonly FrozenDictionary<string, string> Resources = Load();
    public static IReadOnlyDictionary<string, string> All => Resources;
    public static string Text(string code)
    {
        var key = "errors." + code.Replace('_', '.');
        return Resources.TryGetValue(key, out var text) ? text : Resources["errors.request.invalid.detail"];
    }
    private static FrozenDictionary<string, string> Load()
    {
        var assembly = typeof(BackendErrorFallbacks).Assembly;
        var name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(".Localization.BackendErrorFallbacks.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name)!;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
