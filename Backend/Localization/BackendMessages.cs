using System.Collections.Frozen;
using System.Text.Json;

namespace RBBH.ConnectedParties.Localization;

/// <summary>
/// Immutable safe BS fallback when exception construction precedes scoped runtime lookup.
/// Runtime Language Packs remain authoritative; GlobalExceptionHandler resolves the same key.
/// </summary>
public static class BackendMessages
{
    private static readonly FrozenDictionary<string, string> Resources = Load();
    private static readonly AsyncLocal<IReadOnlyDictionary<string, string>?> Current = new();
    public static IReadOnlyDictionary<string, string> All => Resources;

    public static IDisposable Use(IReadOnlyDictionary<string, string> labels)
    {
        var previous = Current.Value; Current.Value = labels; return new Scope(previous);
    }

    public static string Message(string key, params object?[] parameters)
    {
        var fallback = Resources.TryGetValue(key, out var baseline) ? baseline : Resources["errors.request.invalid.detail"];
        var template = Current.Value?.TryGetValue(key, out var label) == true ? label : fallback;
        if (parameters.Length == 0) return template;
        try { return string.Format(System.Globalization.CultureInfo.InvariantCulture, template, parameters); }
        catch (FormatException) { return string.Format(System.Globalization.CultureInfo.InvariantCulture, fallback, parameters); }
    }

    private sealed class Scope(IReadOnlyDictionary<string, string>? previous) : IDisposable
    { public void Dispose() => Current.Value = previous; }

    public static string Text(string code)
    {
        var key = "errors." + code.Replace('_', '.');
        return Resources.TryGetValue(key, out var text) ? text : Resources["errors.request.invalid.detail"];
    }
    private static FrozenDictionary<string, string> Load()
    {
        var assembly = typeof(BackendMessages).Assembly;
        var name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(".Localization.BackendMessages.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name)!;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}
