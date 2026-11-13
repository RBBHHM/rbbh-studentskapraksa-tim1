using RBI.Localization;

namespace RBBH.RelatedPartiesRegister.Web.Components;

// Application wording only. Optional RBI localization owns culture/fallback;
// the plain starter does not acquire localization packages or resource files.
public sealed class AppText
{
    private readonly IRbiLocalizer _localizer;
    private readonly RuntimeResources? _runtime;
    public AppText(IRbiLocalizer localizer, RuntimeResources? runtime = null) { _localizer = localizer; _runtime = runtime; }
    public string this[string key] => _runtime is not null && _runtime.TryGet(key, out var text) ? text : _localizer[key];
    /// <summary>Display name of an application role: the localized "users.role.{name}" entry when one exists, otherwise the role name the backend supplied.</summary>
    public string Role(string role)
    {
        var key = "users.role." + role.ToLowerInvariant();
        var text = this[key];
        return text == key ? role : text;
    }
    /// <summary>Text from the runtime language codebook first, then the static locale fallback. Unknown keys remain visible for diagnostics.</summary>
    public string Resolve(string key, System.Text.Json.JsonElement parameters = default)
    {
        if (!string.IsNullOrWhiteSpace(key) && System.Text.RegularExpressions.Regex.IsMatch(key, "^[a-z0-9]+(?:[.-][a-z0-9]+)*$"))
        {
            var text = this[key];
            if (text != key) return RuntimeResources.Fill(text, parameters);
        }
        return _runtime is not null && _runtime.TryGet(key, out var runtime)
            ? RuntimeResources.Fill(runtime, parameters) : key;
    }
    public string Format(string key, params object?[] arguments)
    {
        if (_runtime is not null && _runtime.TryGet(key, out var value))
        {
            try { return string.Format(System.Globalization.CultureInfo.CurrentUICulture, value, arguments); }
            catch (FormatException) { /* Malformed admin-edited templates safely fall back. */ }
        }
        return _localizer[key, arguments];
    }
}
