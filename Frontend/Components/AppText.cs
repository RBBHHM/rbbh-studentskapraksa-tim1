using RBI.Localization;

namespace RBBH.RelatedPartiesRegister.Web.Components;

// Application wording only. Optional RBI localization owns culture/fallback;
// the plain starter does not acquire localization packages or resource files.
public sealed class AppText
{
    private readonly IRbiLocalizer _localizer;
    private readonly RuntimeResources? _runtime;
    public AppText(IRbiLocalizer localizer, RuntimeResources? runtime = null) { _localizer = localizer; _runtime = runtime; }
    public string? Helper(string key) => _runtime?.Helper(key);
    public string? Placeholder(string key) => _runtime?.Placeholder(key);
    public string this[string key]
    {
        get
        {
            var text = _runtime is not null && _runtime.TryGet(key, out var translated) ? translated : _localizer[key];
            return key is "users.empty-roles" or "users.empty-roles-detail" or "guide.reports.point2"
                or "guide.administration.point1" or "guide.administration.point2"
                ? System.Text.RegularExpressions.Regex.Replace(text, @"\bGuest\b", "Gost", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                : text;
        }
    }
    /// <summary>Display name of an application role: the localized "users.role.{name}" entry when one exists, otherwise the role name the backend supplied.</summary>
    public string Role(string role)
    {
        if (role.Equals("guest", StringComparison.OrdinalIgnoreCase) || role.Equals("gost", StringComparison.OrdinalIgnoreCase)) return "Gost";
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
